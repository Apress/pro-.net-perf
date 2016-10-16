using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using HelperLibrary;
using Microsoft.CSharp;

namespace Ch10
{
    struct LargeStruct
    {
        public long First;
        public int Second;
        public int Third;
        public long Fourth;
        //public int Fifth;
        //public int Sixth;
    }

    public struct Packet
    {
        public uint SourceIP;
        public uint DestIP;
        public ushort SourcePort;
        public ushort DestPort;
        public uint Flags;
        public uint Checksum;
    }

    public struct Point2D
    {
        public int X;
        public int Y;
    }

    public class Program
    {
        #region Miscellaneous

        static int Add(int i, int j)
        {
            return i + j;
        }

        private static uint[] staticArray;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void CopyOut(byte[] data, int offset, out LargeStruct ls)
        {
            fixed (byte* ptr = &data[0])
            {
                LargeStruct* pls = (LargeStruct*)ptr;
                ls = *pls;
            }
        }

        private static void MethodThatTakesAPoint(Point2D pt)
        {
            pt.Y = pt.X ^ pt.Y;
            Console.WriteLine(pt.Y);
        }

        private static int GCD(int a, int b)
        {
            if (b == 0) return a;
            return GCD(b, a % b);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TailCalls()
        {
            GCD(48, 18);
        }

        private static void RangeCheck()
        {
            uint[] array = new uint[100];
            array[4] = 0xBADC0FFE;

            for (int k = 0; k < array.Length; ++k)
            {
                array[k] = (uint)k;
            }
            Console.ReadLine();
            for (int k = 0; k < array.Length - 1; ++k)
            {
                array[k] = (uint)k;
            }
            Console.ReadLine();
            for (int k = 0; k < array.Length - 1; ++k)
            {
                array[k + 1] = (uint)k;
            }
            Console.ReadLine();
            for (int k = 0; k < array.Length / 2; ++k)
            {
                array[k * 2] = (uint)k;
            }
            Console.ReadLine();
            staticArray = array;
            for (int k = 0; k < staticArray.Length; ++k)
            {
                staticArray[k] = (uint)k;
            }
            Console.ReadLine();
            for (int k = 7; k < array.Length; ++k)
            {
                array[k] = (uint)k;
            }
        }

        #endregion

        #region Serialization code-gen

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string XmlSerializeReflection(object obj)
        {
            StringBuilder builder = new StringBuilder();
            Type type = obj.GetType();
            builder.AppendFormat("<{0} Type='{1}'>", type.Name, type.AssemblyQualifiedName);
            builder.AppendLine();
            if (type.IsPrimitive || type == typeof(string))
            {
                builder.Append(obj.ToString());
                builder.AppendLine();
            }
            else
            {
                foreach (FieldInfo field in type.GetFields())
                {
                    object value = field.GetValue(obj);
                    if (value != null)
                    {
                        builder.AppendFormat("<{0}>{1}</{0}>", field.Name, XmlSerializeReflection(value));
                        builder.AppendLine();
                    }
                }
            }
            builder.AppendFormat("</{0}>", type.Name);
            builder.AppendLine();
            return builder.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string XmlSerializeCodeGen<T>(T obj)
        {
            Func<T, string> serializer = XmlSerializationCache<T>.Serializer;
            if (serializer == null)
            {
                serializer = XmlSerializationCache<T>.GenerateSerializer();
            }
            return serializer(obj);
        }

        private static class XmlSerializationCache<T>
        {
            public static Func<T, string> Serializer;
            public static Func<T, string> GenerateSerializer()
            {
                StringBuilder code = new StringBuilder();
                code.AppendLine("using System;");
                code.AppendLine("using System.Text;");
                code.AppendLine("public static class SerializationHelper {");
                code.AppendFormat("public static string XmlSerialize({0} obj) {{", typeof(T).FullName);
                code.AppendLine();
                code.AppendLine("StringBuilder result = new StringBuilder();");
                code.AppendFormat("result.Append(\"<{0} Type='{1}'>\");", typeof(T).Name, typeof(T).AssemblyQualifiedName);
                code.AppendLine();
                code.AppendLine("result.AppendLine();");
                code.AppendLine();
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    code.AppendLine("result.AppendLine(obj.ToString());");
                }
                else
                {
                    foreach (FieldInfo field in typeof(T).GetFields())
                    {
                        code.AppendFormat("result.Append(\"<{0}>\");", field.Name);
                        code.AppendLine();
                        code.AppendFormat("result.Append(Ch10.Program.XmlSerializeCodeGen(obj.{0}));", field.Name);
                        code.AppendLine();
                        code.AppendFormat("result.Append(\"</{0}>\");", field.Name);
                        code.AppendLine();
                    }
                }
                code.AppendFormat("result.Append(\"</{0}>\");", typeof(T).Name);
                code.AppendLine();
                code.AppendLine("result.AppendLine();");
                code.AppendLine("return result.ToString();");
                code.AppendLine("}");
                code.AppendLine("}");

                CSharpCodeProvider compiler = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();
                parameters.ReferencedAssemblies.Add(typeof(T).Assembly.Location);
                parameters.CompilerOptions = "/optimize+";
                CompilerResults results = compiler.CompileAssemblyFromSource(parameters, code.ToString());
                if (results.Errors.HasErrors)
                {
                    foreach (var error in results.Errors)
                        Console.WriteLine(error);
                }
                Type serializationHelper = results.CompiledAssembly.GetType("SerializationHelper");
                MethodInfo method = serializationHelper.GetMethod("XmlSerialize");
                return Serializer = (Func<T, string>)Delegate.CreateDelegate(typeof(Func<T, string>), method);
            }
        }

        private static volatile string s;

        public static void Serialization()
        {
            Point2D point = new Point2D { X = 4, Y = 17 };

            for (int i = 0; i < 5; ++i)
            {
                Stopwatch sw = Stopwatch.StartNew();
                for (int j = 0; j < 100000; ++j)
                {
                    s = XmlSerializeReflection(point);
                }
                Console.WriteLine("Reflection-based: " + sw.ElapsedMilliseconds);
                sw = Stopwatch.StartNew();
                for (int j = 0; j < 100000; ++j)
                {
                    s = XmlSerializeCodeGen(point);
                }
                Console.WriteLine("Codegen-based: " + sw.ElapsedMilliseconds);                
            }
        }

        #endregion

        #region Packet deserialization

        //Supports only some primitive fields, does not recurse
        public static void ReadReflectionBinaryReader<T>(byte[] data, int offset, out T value)
        {
            object box = default(T);
            MemoryStream stream = new MemoryStream(data);
            stream.Seek(offset, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(stream);
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(box, reader.ReadInt32());
                }
                else if (field.FieldType == typeof(uint))
                {
                    field.SetValue(box, reader.ReadUInt32());
                }
                else if (field.FieldType == typeof(short))
                {
                    field.SetValue(box, reader.ReadInt16());
                }
                else if (field.FieldType == typeof(ushort))
                {
                    field.SetValue(box, reader.ReadUInt16());
                }
                else if (field.FieldType == typeof(byte))
                {
                    field.SetValue(box, reader.ReadByte());
                }
                else if (field.FieldType == typeof(sbyte))
                {
                    field.SetValue(box, reader.ReadSByte());
                }
                else if (field.FieldType == typeof(long))
                {
                    field.SetValue(box, reader.ReadInt64());
                }
                else if (field.FieldType == typeof(ulong))
                {
                    field.SetValue(box, reader.ReadUInt64());
                }
            }
            value = (T)box;
        }

        //Supports only some primitive fields, does not recurse
        public static void ReadReflectionBitConverter<T>(byte[] data, int offset, out T value)
        {
            object box = default(T);
            int current = offset;
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(box, BitConverter.ToInt32(data, current));
                    current += 4;
                }
                else if (field.FieldType == typeof(uint))
                {
                    field.SetValue(box, BitConverter.ToUInt32(data, current));
                    current += 4;
                }
                else if (field.FieldType == typeof(short))
                {
                    field.SetValue(box, BitConverter.ToInt16(data, current));
                    current += 2;
                }
                else if (field.FieldType == typeof(ushort))
                {
                    field.SetValue(box, BitConverter.ToUInt16(data, current));
                    current += 2;
                }
                else if (field.FieldType == typeof(byte))
                {
                    field.SetValue(box, data[current]);
                    current += 1;
                }
                else if (field.FieldType == typeof(sbyte))
                {
                    field.SetValue(box, (sbyte)data[current]);
                    current += 1;
                }
                else if (field.FieldType == typeof(long))
                {
                    field.SetValue(box, BitConverter.ToInt64(data, current));
                    current += 8;
                }
                else if (field.FieldType == typeof(ulong))
                {
                    field.SetValue(box, BitConverter.ToUInt64(data, current));
                    current += 8;
                }
            }
            value = (T)box;
        }

        public static void ReadGCHandleMarshalPtrToStructure<T>(byte[] data, int offset, out T value)
        {
            GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = gch.AddrOfPinnedObject();
                ptr += offset;
                value = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                gch.Free();
            }
        }

        public static unsafe void ReadFixedMarshalPtrToStructure<T>(byte[] data, int offset, out T value)
        {
            fixed (byte* ptr = &data[offset])
            {
                value = (T)Marshal.PtrToStructure(new IntPtr(ptr), typeof(T));
            }
        }

        //Inspired by: http://stackoverflow.com/questions/4764573/why-is-typedreference-behind-the-scenes-its-so-fast-and-safe-almost-magical
        //This method takes a 'ref T' unlike all the others that take an 'out T', because if it took
        //an 'out T', we'd have to initialize it before using __makeref, and incur an unnecessary
        //initialization cost.
        //x86 - Not inlined, requires call to JIT_GetRefAny and then four MOVQ instructions and one MOVS instruction
        //x64 - Inlined,     requires call to JIT_GetRefAny and then creates field-by-field local copy (DWORD MOV instructions) and then again into 'value'
        public static unsafe void ReadPointerTypedRef<T>(byte[] data, int offset, ref T value)
        {
            TypedReference tr = __makeref(value); //We aren't actually modifying 'value' -- just need an lvalue to start with
            fixed (byte* ptr = &data[offset])
            {
                *(IntPtr*)&tr = (IntPtr)ptr; //The first pointer-sized field of TypedReference is the object address
                value = __refvalue( tr,T);   //Copy the pointee from the TypedReference to 'value'
            }
        }

        //x86 - Not inlined, compiled to four MOVQ instructions and one MOVS instruction
        //x64 - Inlined,     unrolled (x2), creates field-by-field local copy (DWORD MOV instructions) and then again into 'packet'
        public static unsafe void ReadPointerNonGeneric(byte[] data, int offset, out Packet packet)
        {
            fixed (byte* pData = &data[offset])
            {
                packet = *(Packet*)pData;
            }
        }

        //x86 - Inlined, compiled down to four MOVQ instructions and one MOVS instruction
        //x64 - Inlined, unrolled (x2), creates field-by-field local copy (DWORD MOV instructions) and then again into 'packet'
        public static unsafe void ReadPointerNonGenericFromPointer(byte* data, int offset, out Packet packet)
        {
            packet = *(Packet*)(data + offset);
        }

        public delegate void ReadDelegate<T>(byte[] data, int offset, out T value);

        static class DelegateHolder<T>
        {
            public static ReadDelegate<T> Value;
            public static ReadDelegate<T> CreateDelegate()
            {
                DynamicMethod dm = new DynamicMethod("Read", null,
                    new Type[] { typeof(byte[]), typeof(int), typeof(T).MakeByRefType() },
                    Assembly.GetExecutingAssembly().ManifestModule);
                dm.DefineParameter(1, ParameterAttributes.None, "data");
                dm.DefineParameter(2, ParameterAttributes.None, "offset");
                dm.DefineParameter(3, ParameterAttributes.Out, "value");
                ILGenerator generator = dm.GetILGenerator();
                generator.DeclareLocal(typeof(byte).MakePointerType(), pinned: true);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldelema, typeof(byte));
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Conv_I);
                generator.Emit(OpCodes.Ldobj, typeof(T));
                generator.Emit(OpCodes.Stobj, typeof(T));
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Conv_U);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ret);
                Value = (ReadDelegate<T>)dm.CreateDelegate(typeof(ReadDelegate<T>));
                return Value;
            }
        }

        public static void ReadPointerLCG<T>(byte[] data, int offset, out T value)
        {
            ReadDelegate<T> del = DelegateHolder<T>.Value;
            if (del == null)
            {
                del = DelegateHolder<T>.CreateDelegate();
            }
            del(data, offset, out value);
        }

        public static void ReadBitConverterNonGeneric(byte[] data, int offset, out Packet packet)
        {
            packet.SourceIP = BitConverter.ToUInt32(data, offset);
            packet.DestIP = BitConverter.ToUInt32(data, offset + 4);
            packet.SourcePort = BitConverter.ToUInt16(data, offset + 8);
            packet.DestPort = BitConverter.ToUInt16(data, offset + 10);
            packet.Flags = BitConverter.ToUInt32(data, offset + 12);
            packet.Checksum = BitConverter.ToUInt32(data, offset + 16);
        }

        //x86 - Inlined, compiled down to two MOVQ and one STOS instruction (storing 0s)
        //x64 - Inlined, unrolled (x2), compiled down to three MOV instructions (one QWORD, one DWORD) (storing 0s)
        public static void ReadControlZeroFill<T>(byte[] data, int offset, out T value)
        {
            value = default(T);
        }

        public static void PacketDeserializationMeasurements()
        {
            byte[] data = { 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 4, 0, 5, 0, 0, 0, 6, 0, 0, 0 };
            const int ITERATIONS = 100000;
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS; ++j)
                {
                    ReadReflectionBinaryReader(data, 0, out packet);
                }
            }, "ReflectionBinaryReader", ITERATIONS);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS; ++j)
                {
                    ReadReflectionBitConverter(data, 0, out packet);
                }
            }, "ReflectionBitConverter", ITERATIONS);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 10; ++j)
                {
                    ReadGCHandleMarshalPtrToStructure(data, 0, out packet);
                }
            }, "GCHandleMarshalPtrToStructure*10", ITERATIONS * 10);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 10; ++j)
                {
                    ReadFixedMarshalPtrToStructure(data, 0, out packet);
                }
            }, "FixedMarshalPtrToStructure*10", ITERATIONS * 10);
            Measure(() =>
            {
                Packet packet = new Packet();
                for (int j = 0; j < ITERATIONS * 100; ++j)
                {
                    ReadPointerTypedRef(data, 0, ref packet);
                }
            }, "PointerTypedRef*100", ITERATIONS * 100);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 100; ++j)
                {
                    ReadPointerNonGeneric(data, 0, out packet);
                }
            }, "PointerNonGeneric*100", ITERATIONS * 100);
            Measure(() =>
            {
                Packet packet;
                unsafe
                {
                    fixed (byte* pData = &data[0])
                    {
                        for (int j = 0; j < ITERATIONS * 100; ++j)
                        {
                            ReadPointerNonGenericFromPointer(pData, 0, out packet);
                        }
                    }
                }
            }, "PointerNonGenericFromPointer*100", ITERATIONS * 100);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 100; ++j)
                {
                    ReadPointerLCG(data, 0, out packet);
                }
            }, "PointerLCG*100", ITERATIONS * 100);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 100; ++j)
                {
                    ReadBitConverterNonGeneric(data, 0, out packet);
                }
            }, "BitConverterNonGeneric*100", ITERATIONS * 100);
            Measure(() =>
            {
                Packet packet;
                for (int j = 0; j < ITERATIONS * 100; ++j)
                {
                    ReadControlZeroFill(data, 0, out packet);
                }
            }, "ControlZeroFill*100", ITERATIONS * 100);
            Console.ReadLine();
        }

        #endregion

        #region SIMD and ILP

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void VectorAddDelegate(float[] C, float[] B, float[] A, int length);

        class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize,
               AllocationType flAllocationType, MemoryProtection flProtect);

            [Flags()]
            public enum AllocationType : uint
            {
                COMMIT = 0x1000,
                RESERVE = 0x2000,
                RESET = 0x80000,
                LARGE_PAGES = 0x20000000,
                PHYSICAL = 0x400000,
                TOP_DOWN = 0x100000,
                WRITE_WATCH = 0x200000
            }

            [Flags()]
            public enum MemoryProtection : uint
            {
                EXECUTE = 0x10,
                EXECUTE_READ = 0x20,
                EXECUTE_READWRITE = 0x40,
                EXECUTE_WRITECOPY = 0x80,
                NOACCESS = 0x01,
                READONLY = 0x02,
                READWRITE = 0x04,
                WRITECOPY = 0x08,
                GUARD_Modifierflag = 0x100,
                NOCACHE_Modifierflag = 0x200,
                WRITECOMBINE_Modifierflag = 0x400
            }
        }

        static int a = 12;

        private unsafe static void ILP1()
        {
            int[] afirst = new int[100];
            int[] asecond = new int[100];
            int[] athird = new int[100];

            fixed (int* first = &afirst[0])
            fixed (int* second = &asecond[0])
            fixed (int* third = &athird[0])
                for (int outer = 0; outer < 10; ++outer)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        for (int k = 1; k < 100; ++k)
                        {
                            first[k] = a * second[k] + third[k];
                        }
                    }
                    Console.WriteLine("Best: " + sw.ElapsedMilliseconds);

                    sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        for (int k = 1; k < 100; ++k)
                        {
                            first[k] = a * second[k] + third[k];
                            ++k;
                            first[k] = a * second[k] + third[k];
                        }
                    }
                    Console.WriteLine("Best [unrolled]: " + sw.ElapsedMilliseconds);

                    sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        for (int k = 1; k < 100; ++k)
                        {
                            first[k] = a * second[k] + first[k - 1];
                        }
                    }
                    Console.WriteLine("Standard: " + sw.ElapsedMilliseconds);

                    sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        for (int k = 1; k < 100; ++k)
                        {
                            first[k] = a * first[k - 1] + third[k];
                        }
                    }
                    Console.WriteLine("Poor: " + sw.ElapsedMilliseconds);
                }
        }

        private unsafe static void ILP2()
        {
            int[] aarr = new int[100];

            fixed (int* arr = &aarr[0])
                for (int outer = 0; outer < 10; ++outer)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        int max = arr[0];
                        for (int k = 1; k < 100; ++k)
                        {
                            max = Math.Max(max, arr[k]);
                        }
                        a = max;
                    }
                    Console.WriteLine("Standard: " + sw.ElapsedMilliseconds);

                    sw = Stopwatch.StartNew();
                    for (int iters = 0; iters < 1000000; ++iters)
                    {
                        int max0 = arr[0];
                        int max1 = arr[1];
                        for (int k = 2; k < 100; k += 2)
                        {
                            max0 = Math.Max(max0, arr[k]);
                            max1 = Math.Max(max1, arr[k + 1]);
                        }
                        a = Math.Max(max0, max1);
                    }
                    Console.WriteLine("Better: " + sw.ElapsedMilliseconds);
                }
            Console.ReadLine();
        }

        private static void StandardVsSse()
        {
            float[] A = new float[100];
            float[] B = new float[A.Length];
            float[] C = new float[A.Length];
            Stopwatch sw = Stopwatch.StartNew();
            for (int iters = 0; iters < 10000000; ++iters)
            {
                for (int k = 0; k < A.Length; ++k)
                {
                    C[k] = A[k] + B[k];
                }
            }
            Console.WriteLine("Standard: " + sw.ElapsedMilliseconds);

            byte[] sseAssemblyBytes = { 0x8b, 0x5c, 0x24, 0x10, 0x8b, 0x74, 0x24, 0x0c, 0x8b, 0x7c, 0x24,
                                        0x08, 0x8b, 0x4c, 0x24, 0x04, 0x31, 0xd2, 0x0f, 0x10, 0x0c, 0x97,
                                        0x0f, 0x10, 0x04, 0x96, 0x0f, 0x58, 0xc8, 0x0f, 0x11, 0x0c, 0x91,
                                        0x83, 0xc2, 0x04, 0x39, 0xda, 0x7f, 0xea, 0xc2, 0x10, 0x00 };
            IntPtr codeBuffer = NativeMethods.VirtualAlloc(IntPtr.Zero, new UIntPtr((uint)sseAssemblyBytes.Length),
                NativeMethods.AllocationType.RESERVE | NativeMethods.AllocationType.COMMIT,
                NativeMethods.MemoryProtection.EXECUTE_READWRITE);
            Marshal.Copy(sseAssemblyBytes, 0, codeBuffer, sseAssemblyBytes.Length);
            A[2] = 2.0f;
            B[2] = 3.0f;
            VectorAddDelegate pfunc = (VectorAddDelegate)Marshal.GetDelegateForFunctionPointer(codeBuffer, typeof(VectorAddDelegate));

            sw = Stopwatch.StartNew();
            for (int iters = 0; iters < 10000000; ++iters)
            {
                pfunc(C, A, B, A.Length);
            }
            Console.WriteLine("SSE: " + sw.ElapsedMilliseconds);
        }

        #endregion

        public static void Measure(Action action, string description, int iterations, int repetitions = 5)
        {
            for (int i = 0; i < repetitions; ++i)
            {
                Stopwatch sw = Stopwatch.StartNew();
                action();
                Console.WriteLine("{0} - {1}ms [{2}ms per iteration]", description, sw.ElapsedMilliseconds,
                    sw.ElapsedMilliseconds / (double)iterations);
            }
        }

        static void Main(string[] args)
        {
            ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ProfileOptimization.StartProfile("Standard.prof");

            PacketDeserializationMeasurements();

            Serialization();

            TailCalls();

            Point2D pt;
            pt.X = 3;
            pt.Y = 5; 
            MethodThatTakesAPoint(pt);

            ILP2();
            ILP1();

            byte[] data = new byte[100];
            LargeStruct copy;
            CopyOut(data, 20, out copy);
            Console.WriteLine(copy.Second);
            Console.WriteLine(copy.Third);

            StandardVsSse();

            int i = 4;
            int j = 3*i + 11;
            Console.WriteLine(Add(i, j));

            RangeCheck();

            UtilityClass.SayHello();
            Console.ReadLine();
        }
    }
}
