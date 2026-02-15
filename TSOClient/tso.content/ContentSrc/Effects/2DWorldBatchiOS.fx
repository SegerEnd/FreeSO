#include "LightingCommon.fx"

/**
 * Various effects for rendering the 2D world. (iOS)
 * Uses software depth testing (depthMap) and single COLOR0 output.
 * Modern lighting via LightingCommon.fx.
 */
float4x4 viewProjection : ViewProjection;
float4x4 worldViewProjection : ViewProjection;
float4x4 rotProjection : ViewProjection;
float4x4 iWVP;
float worldUnitsPerTile = 2.5;
float3 dirToFront;
float4 offToBack;
bool depthOutMode;
bool drawingFloor;

float2 PxOffset;
float4 WorldOffset;

float MaxFloor;

texture pixelTexture : Diffuse;
texture depthTexture : Diffuse;
texture maskTexture : Diffuse;
texture ambientLight : Diffuse;
texture depthMap : Diffuse;

sampler pixelSampler = sampler_state {
    texture = <pixelTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler depthSampler = sampler_state {
    texture = <depthTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler maskSampler = sampler_state {
    texture = <maskTexture>;
    AddressU  = CLAMP; AddressV  = CLAMP; AddressW  = CLAMP;
    MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler ambientSampler = sampler_state {
	texture = <ambientLight>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

sampler depthMapSampler = sampler_state {
	texture = <depthMap>;
	AddressU = CLAMP; AddressV = CLAMP; AddressW = CLAMP;
	MIPFILTER = POINT; MINFILTER = POINT; MAGFILTER = POINT;
};

float dpth(float4 v) {
    return v.r;
}

float4 packDepth(float d) {
    float4 enc = float4(1.0, 255.0, 65025.0, 0.0) * d;
    enc = frac(enc);
    enc -= enc.yzww * float4(1.0 / 255.0, 1.0 / 255.0, 1.0 / 255.0, 0.0);
    enc.a = 1;
    return enc;
}

float unpackDepth(float4 d) {
    return dot(d, float4(1.0, 1 / 255.0, 1 / 65025.0, 0));
}

float4 packObjID(float id) {
    return (packDepth(id));
}

// Convert clip-space xy to UV for depth map sampling
float2 sP(float2 v) {
	return ((v*float2(0.5, -0.5)) + float2(0.5, 0.5));
}

// Wall lighting interpolation (advanced lighting)
float4 lightInterp2D(float4 inPosition) {
	inPosition.xyz *= WorldToLightFactor;
	inPosition.xz += LightOffset;

	float level = max(0, floor(inPosition.y) + 0.0001);
	float abvLevel = min(MaxFloor, level + 1);
	float2 iPA = inPosition.xz + 1 / MapLayout * floor(float2(abvLevel % MapLayout.x, abvLevel / MapLayout.x));
	inPosition.xz += 1 / MapLayout * floor(float2(level % MapLayout.x, level / MapLayout.x));

	float4 lTex = tex2D(advLightSampler, inPosition.xz);
	lTex.rgb = lerp(lTex.rgb, tex2D(advLightSampler, iPA).rgb, max(0, (inPosition.y % 1) * 2 - 1));

	return lightColorI(lTex, clamp((inPosition.y % 1) * 3, 0, 1));
}

/**
 * SIMPLE EFFECT
 */

struct SimpleVertex {
    float4 position: SV_Position0;
    float2 texCoords : TEXCOORD0;
    float objectID : TEXCOORD1;
};

SimpleVertex vsSimple(SimpleVertex v){
    SimpleVertex result;
    result.position = mul(v.position, viewProjection);
    result.texCoords = v.texCoords;
    result.objectID = v.objectID;
    return result;
}

void psSimple(SimpleVertex v, out float4 color: COLOR0){
	color = tex2D( pixelSampler, v.texCoords);
	color.rgb *= color.a;
	if (color.a == 0) discard;
}

technique drawSimple {
   pass p0 {
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader = compile ps_3_0 psSimple();
   }
}

void psIDSimple(SimpleVertex v, out float4 color: COLOR0){
	color = packObjID(v.objectID.x);
    color.a = min(tex2D(pixelSampler, v.texCoords).a*255.0, 1.0);
	if (color.a == 0) discard;
}

technique drawSimpleID {
   pass p0 {
        VertexShader = compile vs_3_0 vsSimple();
        PixelShader = compile ps_3_0 psIDSimple();
   }
}


/**
 * SPRITE ZBUFFER EFFECT
 */

struct ZVertexIn {
	float4 position: SV_Position0;
    float2 texCoords : TEXCOORD0;
    float3 worldCoords : TEXCOORD1;
    float2 objectID : TEXCOORD2;
	float2 room : TEXCOORD3;
};

struct ZVertexOut {
	float4 position: SV_Position0;
    float2 texCoords : TEXCOORD0;
    float2 objectID: TEXCOORD2;
    float2 backDepth: TEXCOORD3;
    float2 frontDepth: TEXCOORD4;
	float2 roomVec : TEXCOORD5;
	float4 screenPos : TEXCOORD6;
};

float depthCalc(ZVertexOut v) {
	float difference = (1 - dpth(tex2D(depthSampler, v.texCoords))) / 0.4;
	return (v.backDepth.x + (difference*v.frontDepth.x));
}

float2 depthCalc2(ZVertexOut v) {
	float difference = (1 - dpth(tex2D(depthSampler, v.texCoords))) / 0.4;
	return (v.backDepth + (difference*v.frontDepth));
}

ZVertexOut vsZSprite(ZVertexIn v){
    ZVertexOut result;
	float4 inPos = v.position;
	inPos.xy += PxOffset;
	float4 pos = mul(inPos, viewProjection);
    result.position = pos;
	result.screenPos = float4(pos.xy, sP(pos.xy));
    result.texCoords = v.texCoords;
	result.objectID = v.objectID;
	result.roomVec = v.room;

    //HACK: somehow prevents result.roomVec from failing to set?? Condition should never occur.
    if (v.room.x == 2.0 && v.room.y == 2.0 && v.objectID.x == -1.0) result.texCoords /= 2.0;

    float4 backPosition = float4(v.worldCoords.x, v.worldCoords.y, v.worldCoords.z, 1) + WorldOffset + offToBack;
    float4 frontPosition = float4(backPosition.x, backPosition.y, backPosition.z, backPosition.w);
    frontPosition.x += dirToFront.x;
    frontPosition.z += dirToFront.z;

    float4 backProjection = mul(backPosition, worldViewProjection);
    float4 frontProjection = mul(frontPosition, worldViewProjection);

    result.backDepth.x = backProjection.z / backProjection.w - (0.00000000001*backProjection.x+0.00000000001*backProjection.y);
	if (isnan(result.backDepth.x)) result.backDepth.x = 0;
	result.backDepth.y = backProjection.w;
    result.frontDepth.x = frontProjection.z / frontProjection.w - (0.00000000001*frontProjection.x+0.00000000001*frontProjection.y);
	if (isnan(result.frontDepth.x)) result.frontDepth.x = 0;
	result.frontDepth.y = frontProjection.w;
    result.frontDepth -= result.backDepth;

    return result;
}

ZVertexOut restoreZSprite(ZVertexIn v){
    ZVertexOut result;
    result.position = mul(v.position, viewProjection);
	result.screenPos = float4(result.position.xy, sP(result.position.xy));
    result.texCoords = v.texCoords;
    result.objectID = v.objectID;
    result.roomVec = v.room;

    float4 backPosition = float4(v.worldCoords.x, v.worldCoords.y, v.worldCoords.z, 1);

    float4 backProjection = mul(backPosition, rotProjection);
    float4 nullProjection = mul(float4(0,0,0,1), rotProjection);

    result.backDepth.x = backProjection.z / backProjection.w - (0.00000000001*backProjection.x+0.00000000001*backProjection.y+0.00000000001*nullProjection.x+0.00000000001*nullProjection.y) - nullProjection.z / nullProjection.w;
	result.backDepth.y = 0;
    result.frontDepth = result.backDepth;

    return result;
}

void psZSprite(ZVertexOut v, out float4 color:COLOR) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a == 0) discard;

	bool lastSeg = floor(v.roomVec.y * 256) == 255;
	int xRoom = floor(v.roomVec.x * 256);
	if (lastSeg == true && xRoom == 254) {
		pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
	} else if (lastSeg == true && xRoom == 253) {
		float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
		pixel = float4(gray, gray, gray, pixel.a);
	}
	else if (v.roomVec.x == 0.0) {
		pixel = pixel;
	}
	else {
		pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
	}

	pixel.rgb *= pixel.a;

	color = pixel;
	float2 d = depthCalc2(v);
	float depth = d.x;
	//SOFTWARE DEPTH TEST
	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;
}

void psZWall(ZVertexOut v, out float4 color:COLOR) {
    color = gammaMulSimple(tex2D(pixelSampler, v.texCoords), tex2D(ambientSampler, v.roomVec));
    color.a = tex2D(maskSampler, v.texCoords).a;
	if (color.a == 0) discard;
	color.rgb *= color.a;

	float depth = depthCalc(v);
	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;
}


technique drawZSprite {
	pass p0 {
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZSprite();
   }
}


technique drawZWall {
	pass p0 {
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZWall();
   }
}

/**
 * SPRITE ZBUFFER EFFECT DEPTH CHANNEL
 */

void psZDepthSprite(ZVertexOut v, out float4 color:COLOR0) {
	if (drawingFloor == true && abs(v.texCoords.x - 0.5) > 0.503 - abs(0.5 - v.texCoords.y)) discard;
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a <= 0.01) discard;
	float2 d = depthCalc2(v);
	float depth = d.x;
	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;

    float4 depthB = packDepth(depth);
    if (depthOutMode == true) {
        color = depthB;
    } else {
		bool lastRow = floor(v.roomVec.y * 256) == 255;
		int col = floor(v.roomVec.x * 256);
		if (lastRow == true && col > 252) {
			if (col == 254) pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
			else if (col == 253) {
				float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
				pixel = float4(gray, gray, gray, pixel.a);
			}
			//255 does not light pixel at all.
		}
		else if (v.roomVec.x < 0.0) pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		else if (v.roomVec.x != 0.0) {
			//advanced lighting mode
			float4 projection = mul(float4(v.screenPos.x, v.screenPos.y, d.x*d.y, d.y), iWVP);
			pixel = gammaMul(pixel, lightProcessLevel(projection, v.objectID.y));
			pixel.rgb += projection.yzw * 0.00000000001; //monogame keeps trying to optimise out entire matrix columns
		}
		color = pixel;

        color.rgb *= max(1, v.objectID.x);
        color.rgb *= color.a;
    }
}

void psZDepthSpriteSimple(ZVertexOut v, out float4 color:COLOR0) {
	if (drawingFloor == true && abs(v.texCoords.x - 0.5) > 0.503 - abs(0.5 - v.texCoords.y)) discard;
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a <= 0.01) discard;
	float depth = depthCalc(v);
	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;

	float4 depthB = packDepth(depth);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		bool lastRow = floor(v.roomVec.y * 256) == 255;
		int col = floor(v.roomVec.x * 256);
		if (lastRow == true && col == 254) pixel = float4(float3(1.0, 1.0, 1.0) - pixel.xyz, pixel.a);
		else if (lastRow == true && col == 253) {
			float gray = dot(pixel.xyz, float3(0.2989, 0.5870, 0.1140));
			pixel = float4(gray, gray, gray, pixel.a);
		}
		else if (v.roomVec.x != 0.0) {
			pixel = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		}
		color = pixel;

		color.rgb *= max(1, v.objectID.x);
		color.rgb *= color.a;
	}
}

technique drawZSpriteDepthChannel {
	pass simple {
		VertexShader = compile vs_3_0 vsZSprite();
		PixelShader = compile ps_3_0 psZDepthSpriteSimple();
	}

    pass advLighting {
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZDepthSprite();
    }
}

void psZDepthWall(ZVertexOut v, out float4 color:COLOR0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
    pixel.a = tex2D(maskSampler, v.texCoords).a;
	if (pixel.a <= 0.01) discard;

	float2 d = depthCalc2(v);
	float depth = d.x;

	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;

    float4 depthB = packDepth(depth);
    if (depthOutMode == true) {
        color = depthB;
    }
    else {
		//advanced light
		float4 projection = mul(float4(v.screenPos.x, v.screenPos.y, d.x*d.y, d.y), iWVP);
		projection.y -= v.objectID.x;
		pixel = gammaMul(pixel, lightInterp2D(projection));
		pixel.rgb += projection.yzw * 0.00000000001; //monogame keeps trying to optimise out entire matrix columns
		color = pixel;

        color.rgb *= color.a;
    }
}

void psZDepthWallSimple(ZVertexOut v, out float4 color:COLOR0) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	pixel.a = tex2D(maskSampler, v.texCoords).a;
	if (pixel.a <= 0.01) discard;
	float depth = depthCalc(v);

	if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;

	float4 depthB = packDepth(depth);
	if (depthOutMode == true) {
		color = depthB;
	}
	else {
		color = gammaMulSimple(pixel, tex2D(ambientSampler, v.roomVec));
		color.rgb *= color.a;
	}
}

technique drawZWallDepthChannel {
	pass simple {
		VertexShader = compile vs_3_0 vsZSprite();
		PixelShader = compile ps_3_0 psZDepthWallSimple();
	}

    pass advLighting {
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZDepthWall();
    }
}

/**
 * SPRITE ZBUFFER EFFECT OBJID
 */

void psZIDSprite(ZVertexOut v, out float4 color:COLOR) {
	float4 pixel = tex2D(pixelSampler, v.texCoords);
	if (pixel.a < 0.1) discard;
	float depth = depthCalc(v);

	if (depthOutMode == true) {
		color = packDepth(depth);
	}
	else {
		color = packObjID(v.objectID.x);
	}
}

technique drawZSpriteOBJID {
   pass p0 {
        VertexShader = compile vs_3_0 vsZSprite();
        PixelShader = compile ps_3_0 psZIDSprite();
   }
}

/**
 * SIMPLE EFFECT WITH RESTORE DEPTH
 */

void psSimpleRestoreDepth(ZVertexOut v, out float4 color: COLOR0){
	color = tex2D( pixelSampler, v.texCoords);

	if (color.a < 0.01) {
		discard;
	}
	else {
		float4 dS = tex2D(depthSampler, v.texCoords);
		float depth = v.backDepth.x + unpackDepth(dS);
		if (depthOutMode == false && unpackDepth(tex2D(depthMapSampler, v.screenPos.zw)) < depth) discard;
		if (depthOutMode == true) {
			color = packDepth(depth);
		}
	}
}

technique drawSimpleRestoreDepth {
   pass p0 {
        VertexShader = compile vs_3_0 restoreZSprite();
        PixelShader = compile ps_3_0 psSimpleRestoreDepth();
   }
}
