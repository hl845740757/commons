using System;
using Wjybxx.Commons;

namespace Wjybxx.Dson.Codec.Codecs
{
public class SerializeRefCodec<T> : IDsonCodec<SerializeRef<T>> where T : class
{
    public void WriteObject(IDsonObjectWriter writer, SerializeRef<T> inst, Type declaredType, SerializeFeatures features) {
        if (inst.Value == null) {
            writer.WriteNull();
        } else {
            writer.WriteObject(inst.Value, SerializeFeatures.SerializeReference);
        }
    }

    public SerializeRef<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        var box = new SerializeRef<T>();
        if (reader.CurrentDsonType == DsonType.Pointer) {
            var ptr = reader.ReadPtr();
            reader.DeferReference(this, box, "Value", (int)ptr.LocalId);
        } else {
            box.Value = reader.ReadObject<T>();
        }
        return box;
    }

    public void SetField(object inst, string name, object value) {
        if (inst is SerializeRef<T> box) {
            box.Value = (T)value;
        }
    }
}
}