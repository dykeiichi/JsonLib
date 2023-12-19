using System;
using Unsafe = System.Runtime.CompilerServices.Unsafe;
using System.Runtime.InteropServices;
using System.Text;

namespace JSON.Entities
{
    public enum JsonType {
        Null,
        String,
        Number,
        Object,
        Array,
        Boolean,
    }

    internal static class Characters {
        internal const byte Zero = (byte)'0';
        internal const byte One = (byte)'1';
        internal const byte Two = (byte)'2';
        internal const byte Three = (byte)'3';
        internal const byte Four = (byte)'4';
        internal const byte Five = (byte)'5';
        internal const byte Six = (byte)'6';
        internal const byte Seven = (byte)'7';
        internal const byte Eight = (byte)'8';
        internal const byte Nine = (byte)'9';
        internal const byte Dot = (byte)'.';
        internal const byte Space = (byte)' ';
        internal const byte N = (byte)'n';
        internal const byte T = (byte)'t';
        internal const byte F = (byte)'f';
        internal const byte Tab = (byte)'\t';
        internal const byte CR = (byte)13;
        internal const byte LF = (byte)10;
        internal const byte Colon = (byte)':';
        internal const byte Coma = (byte)',';
        internal const byte Slash = (byte)'\\';
        internal const byte DoubleQuote = (byte)'"';
        internal const byte OpenKey = (byte)'{';
        internal const byte CloseKey = (byte)'}';
        internal const byte OpenSquareBracket = (byte)'[';
        internal const byte CloseSquareBracket = (byte)']';
    }

    public class InvalidJsonException : Exception {
        public InvalidJsonException() : base("Error, invalid json") { }
    }


    public ref struct JSONObject {

        private enum State {
            StartReading,
            ExpectingKey,
            ReadingKey,
            ExpectingColon,
            ExpectingValue,
            ReadingValue,
            ExpectingComma,
            FinishReading,
        }

        public JSONObject(Span<byte> Data) {
            Keys = new();
            this.Data = Data;
        }

        private Span<byte> Data_;
        private Dictionary<string, (JsonType, uint start, uint stop)> Keys;
        public Span<byte> Data {
            set {
                State ActualState = State.StartReading;
                Data_ = value;
                ref byte SearchSpace = ref MemoryMarshal.GetReference(Data_);
                JsonType TypeActualValue = JsonType.Null;
                StringBuilder SB = new();
                bool HandlingEscapeChar = false;
                uint ActualStart = 0;
                uint ActualStop;
                uint NestedLevels = 0;
                for (uint i = 0; i< Data_.Length; i++) {
                    byte ActualChar;
                    switch (ActualState)
                    {
                        // Starting Reading Json Data
                        case State.StartReading:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                case Characters.OpenKey:
                                    ActualState = State.ExpectingKey;
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Expecting to Read a Key or Finish reading Json Data
                        case State.ExpectingKey:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                case Characters.DoubleQuote:
                                    ActualState = State.ReadingKey;
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Reading Key
                        case State.ReadingKey:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                    break;
                                case Characters.DoubleQuote:
                                    if (HandlingEscapeChar)
                                    {
                                        HandlingEscapeChar = false;
                                        SB.Append('"');
                                    }
                                    else
                                    {
                                        ActualState = State.ExpectingColon;
                                    }
                                    break;
                                case Characters.Slash:
                                    HandlingEscapeChar = true;
                                    SB.Append('\\');
                                    break;
                                case Characters.CloseKey:
                                    if (HandlingEscapeChar)
                                    {
                                        HandlingEscapeChar = false;
                                        SB.Append('}');
                                    }
                                    else
                                    {
                                        ActualState = State.FinishReading;
                                    }
                                    break;
                                default:
                                    HandlingEscapeChar = false;
                                    SB.Append((char)ActualChar);
                                    break;
                            }
                            break;
                        // Expecting Blank character or Colon
                        case State.ExpectingColon:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                case Characters.Colon:
                                    ActualState = State.ExpectingValue;
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Expecting Some Value
                        case State.ExpectingValue:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                case Characters.DoubleQuote:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.String;
                                    ActualStart = i + 1u;
                                    break;
                                case Characters.Zero:
                                case Characters.One:
                                case Characters.Two:
                                case Characters.Three:
                                case Characters.Four:
                                case Characters.Five:
                                case Characters.Six:
                                case Characters.Seven:
                                case Characters.Eight:
                                case Characters.Nine:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.Number;
                                    ActualStart = i;
                                    break;
                                case Characters.N:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.Null;
                                    break;
                                case Characters.OpenKey:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.Object;
                                    ActualStart = i;
                                    break;
                                case Characters.OpenSquareBracket:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.Array;
                                    ActualStart = i;
                                    break;
                                case Characters.T:
                                case Characters.F:
                                    ActualState = State.ReadingValue;
                                    TypeActualValue = JsonType.Boolean;
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Reading Value
                        case State.ReadingValue:
                            switch (TypeActualValue) {
                                case JsonType.Null:
                                    if (Unsafe.Add(ref SearchSpace, i) == (byte)'u') {
                                        if (Unsafe.Add(ref SearchSpace, i + 1) == (byte)'l') {
                                            if (Unsafe.Add(ref SearchSpace, i + 2) == (byte)'l') {
                                                ActualStart = i - 1u;
                                                ActualStop = i + 2u;
                                                Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                SB.Clear();
                                                ActualState = State.ExpectingComma;
                                                i += 2;
                                            }
                                        }
                                    }
                                    break;
                                case JsonType.String:
                                    ActualChar = Unsafe.Add(ref SearchSpace, i);
                                    switch (ActualChar)
                                    {
                                        case Characters.Space:
                                        case Characters.Tab:
                                            break;
                                        case Characters.DoubleQuote:
                                            if (HandlingEscapeChar) { HandlingEscapeChar = false; }
                                            else {
                                                ActualStop = i - 1u;
                                                Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                SB.Clear();
                                                ActualState = State.ExpectingComma;
                                            }
                                            break;
                                        case Characters.Slash:
                                            HandlingEscapeChar = true;
                                            break;
                                        default:
                                            HandlingEscapeChar = false;
                                            break;
                                    }
                                    break;
                                case JsonType.Number:
                                    ActualChar = Unsafe.Add(ref SearchSpace, i);
                                    switch (ActualChar)
                                    {
                                        case Characters.Space:
                                        case Characters.Tab:
                                        case Characters.CR:
                                        case Characters.LF:
                                            ActualStop = i - 1u;
                                            Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                            SB.Clear();
                                            ActualState = State.ExpectingComma;
                                            break;
                                        case Characters.Coma:
                                            ActualStop = i - 1u;
                                            Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                            SB.Clear();
                                            ActualState = State.ExpectingKey;
                                            break;
                                        default:
                                            HandlingEscapeChar = false;
                                            break;
                                    }
                                    break;
                                case JsonType.Object:
                                    ActualChar = Unsafe.Add(ref SearchSpace, i);
                                    switch (ActualChar)
                                    {
/*
                                        case Characters.Space:
                                        case Characters.Tab:
                                        case Characters.CR:
                                        case Characters.LF:
                                            break;
*/
                                        case Characters.OpenKey:
                                            NestedLevels += 1;
                                            break;
                                        case Characters.CloseKey:
                                            if (NestedLevels == 0) {
                                                ActualStop = i;
                                                Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                SB.Clear();
                                                NestedLevels = 0;
                                                ActualState = State.ExpectingComma;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                case JsonType.Array:
                                    ActualChar = Unsafe.Add(ref SearchSpace, i);
                                    switch (ActualChar)
                                    {
/*
                                        case Characters.Space:
                                        case Characters.Tab:
                                        case Characters.CR:
                                        case Characters.LF:
                                            break;
*/
                                        case Characters.OpenSquareBracket:
                                            NestedLevels += 1;
                                            break;
                                        case Characters.CloseSquareBracket:
                                            if (NestedLevels == 0) {
                                                ActualStop = i;
                                                Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                SB.Clear();
                                                NestedLevels = 0;
                                                ActualState = State.ExpectingComma;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                case JsonType.Boolean:

                                    if (Unsafe.Add(ref SearchSpace, i - 1) == (byte)'t') {
                                        if (Unsafe.Add(ref SearchSpace, i) == (byte)'r') {
                                            if (Unsafe.Add(ref SearchSpace, i + 1) == (byte)'u') {
                                                if (Unsafe.Add(ref SearchSpace, i + 2) == (byte)'e') {
                                                    ActualStart = i - 1u;
                                                    ActualStop = i + 2u;
                                                    Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                    SB.Clear();
                                                    ActualState = State.ExpectingComma;
                                                    i += 2;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (Unsafe.Add(ref SearchSpace, i - 1) == (byte)'f') {
                                        if (Unsafe.Add(ref SearchSpace, i) == (byte)'a') {
                                            if (Unsafe.Add(ref SearchSpace, i + 1) == (byte)'l') {
                                                if (Unsafe.Add(ref SearchSpace, i + 2) == (byte)'s') {
                                                    if (Unsafe.Add(ref SearchSpace, i + 3) == (byte)'e') {
                                                        ActualStart = i - 1u;
                                                        ActualStop = i + 3u;
                                                        Keys.Add(SB.ToString(), (TypeActualValue, ActualStart, ActualStop));
                                                        SB.Clear();
                                                        ActualState = State.ExpectingComma;
                                                        i += 3;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Expecting to Read a Comma or Finish reading Json Data
                        case State.ExpectingComma:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                case Characters.Coma:
                                    ActualState = State.ExpectingKey;
                                    break;
                                case Characters.CloseKey:
                                    ActualState = State.FinishReading;
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                        // Finished Reading Json Data
                        case State.FinishReading:
                            ActualChar = Unsafe.Add(ref SearchSpace, i);
                            switch (ActualChar)
                            {
                                case Characters.Space:
                                case Characters.Tab:
                                case Characters.CR:
                                case Characters.LF:
                                    break;
                                default:
                                    throw new InvalidJsonException();
                            }
                            break;
                    }
                }
            }
        }

        public (JsonType, uint, uint) GetIndex(string Key) {
            if (Keys.ContainsKey(Key)) {
                return Keys[Key];
            }
            return (JsonType.Null, 0, 0);
        }

        public byte[] GetValue(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType _, uint Start, uint Stop) Points = GetIndex(Key);
                return Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray();
            }
            return new byte[0];
        }

        public string? GetString(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.String) {
                    return Encoding.ASCII.GetString(Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray());
                }
            }
            return null;
        }

        public long? GetInteger(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Number) {
                    if (long.TryParse(Encoding.ASCII.GetString(Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray()), out long Result))
                    return Result;
                }
            }
            return null;
        }

        public ulong? GetUnsignedInteger(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Number) {
                    if (ulong.TryParse(Encoding.ASCII.GetString(Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray()), out ulong Result))
                    return Result;
                }
            }
            return null;
        }

        public decimal? GetDecimal(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Number) {
                    if (decimal.TryParse(Encoding.ASCII.GetString(Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray()), out decimal Result))
                    return Result;
                }
            }
            return null;
        }

        public byte[]? GetObject(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Object) {
                    return Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray();
                }
            }
            return null;
        }

        public byte[]? GetArray(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Array) {
                    return Data_.Slice((int)Points.Start, (int)(Points.Stop - Points.Start + 1)).ToArray();
                }
            }
            return null;
        }

        public bool? GetBoolean(string Key) {
            if (Keys.ContainsKey(Key)) {
                (JsonType JsonType, uint Start, uint Stop) Points = GetIndex(Key);
                if (Points.JsonType == JsonType.Array) {
                    return Data_[(int)Points.Start] == Characters.T;
                }
            }
            return null;
        }
    }

}

