using FSO.Common.DependencyInjection;

namespace FSO.Common.Serialization
{
    public interface ISerializationContext
    {
        IKernel Kernel { get; }
        IModelSerializer ModelSerializer { get; }
    }
}
