#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Runtime.InteropServices;

namespace BTree.Tests;

[StructLayout(LayoutKind.Explicit)]
public struct Value
{
    [FieldOffset(0)] public readonly ValueType type;
    [FieldOffset(1)] public int intVal;
    [FieldOffset(1)] public long longVal;
    [FieldOffset(1)] public float floatVal;
    [FieldOffset(1)] public double doubleVal;
    [FieldOffset(1)] public bool boolVal;
    [FieldOffset(16)] public object obj;

    public Value(ValueType type) {
        this.type = type;
    }
}

public enum ValueType : byte
{
    Undefine = 0, // key不存在
    Null = 1, // key存在，但value为null -- 可用于支持nullable
    Int = 2,
    Long = 3,
    Float = 4,
    Double = 5,
    Bool = 6,
    Object = 15
}

public interface IKey
{
    string Name { get; }
    int Id { get; }
}

public abstract class Key<T> : IKey, IEquatable<Key<T>>, IComparable<Key<T>>
{
    protected readonly string _name;
    protected readonly int _id;

    protected Key(string name, int id) {
        _name = name;
        _id = id;
    }

    public string Name => _name;
    public int Id => _id;

    public abstract T Unbox(Value value);

    public abstract Value Box(T value);

    #region Equals

    public bool Equals(Key<T>? other) {
        return ReferenceEquals(this, other);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() {
        return _id;
    }

    public static bool operator ==(Key<T>? left, Key<T>? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Key<T>? left, Key<T>? right) {
        return !Equals(left, right);
    }

    public int CompareTo(Key<T>? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _id.CompareTo(other._id);
    }

    #endregion
}

public sealed class IntKey : Key<int>
{
    public IntKey(string name, int id) : base(name, id) {
    }

    public override int Unbox(Value value) => value.intVal;

    public override Value Box(int value) => new Value(ValueType.Int) { intVal = value };
}