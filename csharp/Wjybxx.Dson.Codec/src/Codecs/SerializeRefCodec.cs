using System;
using System.Diagnostics;
using Wjybxx.Commons;

namespace Wjybxx.Dson.Codec.Codecs
{
public class SerializeRefCodec<T> : IDsonCodec<SerializeRef<T>> where T : class
{
    public void WriteObject(IDsonObjectWriter writer, SerializeRef<T> inst, Type declaredType, SerializeFeatures features) {
        if (inst.Value == null) {
            writer.WriteNull();
        } else {
            features |= SerializeFeatures.SerializeReference;
            writer.WriteObject(inst.Value, features);
        }
    }

    public SerializeRef<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        var box = new SerializeRef<T>();
        if (reader.TryReadPtr(out int ptr)) {
            reader.DeferReference(ptr, this, box, "Value");
        } else {
            box.Value = reader.ReadObject<T>();
        }
        return box;
    }

    public void SetField(SerializeRef<T> inst, string _, object value) {
        inst.Value = (T)value;
    }
}
}