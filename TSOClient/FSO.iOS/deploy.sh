#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# --- Configuration ---
CONFIG="Debug"
CLEAN=false
NO_LAUNCH=false

# --- Parse arguments ---
for arg in "$@"; do
    case "$arg" in
        --release)    CONFIG="Release" ;;
        --clean)      CLEAN=true ;;
        --no-launch)  NO_LAUNCH=true ;;
        --help|-h)
            echo "Usage: ./deploy.sh [--release] [--clean] [--no-launch]"
            echo "  --release    Build in Release configuration (default: Debug)"
            echo "  --clean      Clean before building"
            echo "  --no-launch  Install only, don't launch the app"
            exit 0
            ;;
        *)
            echo "Unknown option: $arg"
            echo "Run ./deploy.sh --help for usage"
            exit 1
            ;;
    esac
done

# --- Color helpers ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

pass() { echo -e "  ${GREEN}✓${NC} $1"; }
fail() { echo -e "  ${RED}✗${NC} $1"; }
warn() { echo -e "  ${YELLOW}!${NC} $1"; }

# =============================================================================
# 1. Prerequisite validation
# =============================================================================
echo "Checking prerequisites..."
PREREQ_OK=true

# --- Xcode ---
XCODE_PATH=$(xcode-select -p 2>/dev/null || true)
if [ -z "$XCODE_PATH" ]; then
    fail "Xcode command line tools not found. Run: xcode-select --install"
    PREREQ_OK=false
elif [[ "$XCODE_PATH" != *"Xcode"*"Contents/Developer"* ]]; then
    fail "xcode-select points to '$XCODE_PATH' (not Xcode.app)"
    echo "       Run: sudo xcode-select -s /Applications/Xcode.app/Contents/Developer"
    PREREQ_OK=false
else
    XCODE_VER=$(xcodebuild -version 2>/dev/null | head -1 || echo "unknown")
    pass "Xcode: $XCODE_VER"
fi

# --- .NET SDK ---
DOTNET=""

# Search common homebrew dotnet locations, then fall back to PATH
# Prefer unversioned "dotnet" (latest) over pinned "dotnet@N" versions
for candidate in \
    /opt/homebrew/Cellar/dotnet/*/bin/dotnet \
    /usr/local/Cellar/dotnet/*/bin/dotnet \
    /opt/homebrew/Cellar/dotnet@*/*/bin/dotnet \
    /usr/local/Cellar/dotnet@*/*/bin/dotnet; do
    if [ -x "$candidate" ] 2>/dev/null; then
        DOTNET="$candidate"
        break
    fi
done

if [ -z "$DOTNET" ] && command -v dotnet &>/dev/null; then
    DOTNET="$(command -v dotnet)"
fi

if [ -z "$DOTNET" ]; then
    fail ".NET SDK not found. Install with: brew install dotnet"
    PREREQ_OK=false
else
    DOTNET_VER=$("$DOTNET" --version 2>/dev/null || echo "unknown")
    pass ".NET SDK: $DOTNET_VER ($DOTNET)"
fi

# --- .NET iOS workload ---
if [ -n "$DOTNET" ]; then
    if "$DOTNET" workload list 2>/dev/null | grep -q "^ios "; then
        pass ".NET iOS workload installed"
    else
        fail ".NET iOS workload not installed"
        echo "       Run: $DOTNET workload install ios"
        PREREQ_OK=false
    fi
fi

# --- Code signing identity ---
IDENTITIES=$(security find-identity -v -p codesigning 2>/dev/null || true)
if echo "$IDENTITIES" | grep -q "valid identities found"; then
    IDENTITY_COUNT=$(echo "$IDENTITIES" | grep "valid identities found" | awk '{print $1}')
    if [ "$IDENTITY_COUNT" -gt 0 ] 2>/dev/null; then
        pass "Code signing: $IDENTITY_COUNT identity(s) available"
    else
        fail "No valid code signing identities found"
        echo "       Open Xcode → Settings → Accounts and add your Apple ID"
        PREREQ_OK=false
    fi
else
    warn "Could not check code signing identities"
fi

if [ "$PREREQ_OK" = false ]; then
    echo ""
    echo -e "${RED}Prerequisites not met. Fix the issues above and retry.${NC}"
    exit 1
fi

# =============================================================================
# 2. Device detection via JSON
# =============================================================================
echo ""
echo "Detecting connected iPhone..."

DEVICE_JSON=$(mktemp)
trap "rm -f '$DEVICE_JSON'" EXIT

if ! xcrun devicectl list devices --json-output "$DEVICE_JSON" 2>/dev/null; then
    fail "Failed to run devicectl. Is Xcode installed correctly?"
    exit 1
fi

# Parse JSON with python3 (ships with macOS, no jq dependency)
read -r DEVICE_ID DEVICE_NAME < <(python3 -c "
import json, sys

with open('$DEVICE_JSON') as f:
    data = json.load(f)

devices = data.get('result', {}).get('devices', [])

for d in devices:
    conn = d.get('connectionProperties', {})
    pairing = conn.get('pairingState', '')
    transport = conn.get('transportType', '')
    if pairing == 'paired' and transport == 'wired':
        uid = d.get('identifier', '')
        name = d.get('deviceProperties', {}).get('name', 'Unknown')
        print(uid + ' ' + name)
        sys.exit(0)

# No wired device found — fall back to any paired device
for d in devices:
    conn = d.get('connectionProperties', {})
    pairing = conn.get('pairingState', '')
    if pairing == 'paired':
        uid = d.get('identifier', '')
        name = d.get('deviceProperties', {}).get('name', 'Unknown')
        print(uid + ' ' + name)
        sys.exit(0)

print('')
" 2>/dev/null || echo "")

if [ -z "$DEVICE_ID" ]; then
    fail "No paired iPhone found."
    echo ""
    echo "Available devices:"
    python3 -c "
import json
with open('$DEVICE_JSON') as f:
    data = json.load(f)
devices = data.get('result', {}).get('devices', [])
if not devices:
    print('  (none)')
else:
    for d in devices:
        name = d.get('deviceProperties', {}).get('name', 'Unknown')
        uid = d.get('identifier', '?')
        conn = d.get('connectionProperties', {})
        pairing = conn.get('pairingState', 'unknown')
        transport = conn.get('transportType', 'unknown')
        print(f'  {name}: {uid}')
        print(f'    pairing={pairing}, transport={transport}')
" 2>/dev/null || true
    echo ""
    echo "Make sure your iPhone is connected via USB and paired."
    exit 1
fi

pass "Device: $DEVICE_NAME ($DEVICE_ID)"

# =============================================================================
# 3. Clean (optional)
# =============================================================================
if [ "$CLEAN" = true ]; then
    echo ""
    echo "Cleaning..."
    "$DOTNET" clean -c "$CONFIG" || true
    rm -rf bin/ obj/
fi

# =============================================================================
# 4. Build
# =============================================================================
echo ""
echo "Building FSO.iOS ($CONFIG)..."
"$DOTNET" build \
    -c "$CONFIG" \
    -p:ValidateXcodeVersion=false

# =============================================================================
# 5. Locate .app bundle and extract bundle ID
# =============================================================================
APP_PATH=$(find "$SCRIPT_DIR/bin/$CONFIG/net10.0-ios/ios-arm64" -name "*.app" -type d 2>/dev/null | head -1)

if [ -z "$APP_PATH" ]; then
    fail "Could not find .app bundle in bin/$CONFIG/net10.0-ios/ios-arm64/"
    echo "  Build may have failed or output path differs."
    exit 1
fi

BUNDLE_ID=$(/usr/libexec/PlistBuddy -c "Print CFBundleIdentifier" "$APP_PATH/Info.plist" 2>/dev/null || true)

if [ -z "$BUNDLE_ID" ]; then
    fail "Could not extract bundle ID from $APP_PATH/Info.plist"
    exit 1
fi

echo "App bundle: $APP_PATH"
echo "Bundle ID:  $BUNDLE_ID"

# =============================================================================
# 6. Install on device
# =============================================================================
echo ""
echo "Installing on device..."
xcrun devicectl device install app --device "$DEVICE_ID" "$APP_PATH"

# =============================================================================
# 7. Launch on device
# =============================================================================
if [ "$NO_LAUNCH" = true ]; then
    echo ""
    echo -e "${GREEN}Done!${NC} App installed on $DEVICE_NAME (launch skipped)."
    exit 0
fi

echo ""
echo "Launching on device..."
xcrun devicectl device process launch --terminate-existing --device "$DEVICE_ID" "$BUNDLE_ID"

echo ""
echo -e "${GREEN}Done!${NC} FreeSO is running on $DEVICE_NAME."
