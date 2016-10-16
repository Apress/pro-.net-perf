using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Xml.Serialization;
using System.IO;
using System.Data.Objects;
using System.Diagnostics;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace SerializerTests
{
    public class MemoryStreamNoClose : MemoryStream
    {
        protected override void Dispose(bool disposing)
        {
            
        }

        public override void Close()
        {
            
        }
    }

    [Serializable]
    [DataContract(IsReference=false)]
    public class Root
    {
        [DataMember]
        public SerializedClassA[] Items { get; set; }
        [DataMember]
        public String X { get; set; }
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class SerializedClassA
    {
        [DataMember]
        public string FieldA { get; set; }
        [DataMember]
        public string FieldB { get; set; }
        [DataMember]
        public string FieldC { get; set; }
        [DataMember]
        public string FieldD { get; set; }
        [DataMember]
        public string FieldE { get; set; }
        [DataMember]
        public string FieldF { get; set; }
        [DataMember]
        public string FieldG { get; set; }
        [DataMember]
        public string FieldH { get; set; }
        [DataMember]
        public string FieldI { get; set; }
        [DataMember]
        public string FieldJ { get; set; }
        [DataMember]
        public string FieldK { get; set; }
        [DataMember]
        public string[] FieldL { get; set; }
        [DataMember]
        public string[] FieldM { get; set; }
        [DataMember]
        public string[] FieldN { get; set; }
        [DataMember]
        public double FieldO { get; set; }
        [DataMember]
        public double FieldP { get; set; }
        [DataMember]
        public double[] FieldQ { get; set; }
        [DataMember]
        public double[] FieldR { get; set; }
        [DataMember]
        public string[] FieldS { get; set; }
        [DataMember]
        public SerializedClassB[] FieldT { get; set; }
        [DataMember]
        public SerializedClassC[] FieldU { get; set; }
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class SerializedClassB
    {
        [DataMember]
        public string FieldA { get; set; }
        [DataMember]
        public string FieldB { get; set; }
        [DataMember]
        public string FieldC { get; set; }
        [DataMember]
        public string FieldD { get; set; }
        [DataMember]
        public string[] FieldE { get; set; }
        [DataMember]
        public SerializedClassD FieldF { get; set; }
        [DataMember]
        public SerializedClassE FieldG { get; set; }
        [DataMember]
        public SerializedClassA FieldH { get; set; }
        [DataMember]
        public SerializedClassA FieldI { get; set; }
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class SerializedClassC
    {
        [DataMember]
        public double FieldA { get; set; }
        [DataMember]
        public double FieldB { get; set; }
        [DataMember]
        public double FieldC { get; set; }
        [DataMember]
        public string FieldD { get; set; }
        [DataMember]
        public string[] FieldE { get; set; }
        [DataMember]
        public SerializedClassD FieldF { get; set; }
        [DataMember]
        public SerializedClassE FieldG { get; set; }
        [DataMember]
        public SerializedClassA FieldH { get; set; }
        [DataMember]
        public SerializedClassA FieldI { get; set; }
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class SerializedClassD
    {
        [DataMember]
        public string[] FieldA { get; set; }
        [DataMember]
        public SerializedClassA[] Hello1 { get; set;}
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class SerializedClassE
    {
        [DataMember]
        public string[] FieldA { get; set; }
        [DataMember]
        public SerializedClassA[] Hello2 { get; set; }
    }

    class Program
    {
        static Random _rng = new Random(5);

        static string GetRandomString()
        {
            int len = _rng.Next(64) + 1;
            char[] chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = (char) ('A' +_rng.Next(26));
            }

            return new string(chars);
        }

        static string[] GetRandomStringArray()
        {
            int count = _rng.Next(32) + 1;
            string[] strings = new string[count];
            for (int i = 0; i < count; i++)
            {
                strings[i] = GetRandomString();
            }

            return strings;
        }

        static double[] GetRandomDoubleArray()
        {
            int count = _rng.Next(32) + 1;
            double[] nums = new double[count];
            for (int i = 0; i < count; i++)
            {
                nums[i] = _rng.NextDouble();
            }

            return nums;
        }

        static void Serialize1(Root r, Stream fs, Stopwatch sw)
        {
            XmlSerializer xs = new XmlSerializer(typeof(Root));
            sw.Start();
            xs.Serialize(fs, r);
            sw.Stop();
        }

        static Root DeSerialize1(Stream fs, Stopwatch sw)
        {
            XmlSerializer xs = new XmlSerializer(typeof(Root));
            sw.Start();
            Root r = (Root) xs.Deserialize(fs);
            sw.Stop();
            return r;
        }

        static void Serialize2(Root r, Stream fs, Stopwatch sw)
        {
            XmlSerializer xs = new XmlSerializer(typeof(Root));
            var bindict = XmlDictionaryWriter.CreateBinaryWriter(fs);
            sw.Start();
            xs.Serialize(bindict, r);
            sw.Stop();
            bindict.Close();
        }

        static Root DeSerialize2(Stream fs, Stopwatch sw)
        {
            XmlSerializer xs = new XmlSerializer(typeof(Root));
            var bindict = XmlDictionaryReader.CreateBinaryReader(fs,  XmlDictionaryReaderQuotas.Max);
            sw.Start();
            Root r = (Root) xs.Deserialize(bindict);
            sw.Stop();
            bindict.Close();
            return r;
        }

        static void Serialize3(Root r, Stream fs, Stopwatch sw)
        {
            BinaryFormatter bf = new BinaryFormatter();
            sw.Start();
            bf.Serialize(fs, r);
            sw.Stop();
        }

        static Root DeSerialize3(Stream fs, Stopwatch sw)
        {
            BinaryFormatter bf = new BinaryFormatter();
            sw.Start();
            Root r = (Root) bf.Deserialize(fs);
            sw.Stop();
            return r;
        }

        static void Serialize4(Root r, Stream fs, Stopwatch sw)
        {
            SoapFormatter sf = new SoapFormatter();
            sw.Start();
            sf.Serialize(fs, r);
            sw.Stop();
        }

        static Root DeSerialize4(Stream fs, Stopwatch sw)
        {
            SoapFormatter sf = new SoapFormatter();
            sw.Start();
            Root r = (Root) sf.Deserialize(fs);
            sw.Stop();
            return r;
        }

        static void Serialize5(Root r, Stream fs, Stopwatch sw)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Root));
            XmlDictionaryWriter xw = XmlDictionaryWriter.CreateTextWriter(fs);
            sw.Start();
            ser.WriteObject(xw, r);
            sw.Stop();
            xw.Close();
        }

        static Root DeSerialize5(Stream fs, Stopwatch sw)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Root));
            XmlDictionaryReader xr = XmlDictionaryReader.CreateTextReader(fs, XmlDictionaryReaderQuotas.Max);
            sw.Start();
            var obj = (Root) ser.ReadObject(xr);
            sw.Stop();
            return obj;
        }

        static void Serialize6(Root r, Stream fs, Stopwatch sw)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Root));
            XmlDictionaryWriter xw = XmlDictionaryWriter.CreateBinaryWriter(fs);
            sw.Start();
            ser.WriteObject(xw, r);
            sw.Stop();
            xw.Close();
        }

        static Root DeSerialize6(Stream fs, Stopwatch sw)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Root));
            XmlDictionaryReader xr = XmlDictionaryReader.CreateBinaryReader(fs, XmlDictionaryReaderQuotas.Max);
            sw.Start();
            var obj = (Root)ser.ReadObject(xr);
            sw.Stop();
            return obj;
        }

        static void Serialize7(Root r, Stream fs, Stopwatch sw)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            XmlDictionaryWriter xw = XmlDictionaryWriter.CreateTextWriter(fs);
            sw.Start();
            ser.WriteObject(xw, r);
            sw.Stop();
            xw.Close();
        }

        static Root DeSerialize7(Stream fs, Stopwatch sw)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            XmlDictionaryReader xr = XmlDictionaryReader.CreateTextReader(fs, XmlDictionaryReaderQuotas.Max);
            sw.Start();
            var obj = (Root)ser.ReadObject(xr);
            sw.Stop();
            return obj;
        }

        static void Serialize8(Root r, Stream fs, Stopwatch sw)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            sw.Start();
            XmlDictionaryWriter xw = XmlDictionaryWriter.CreateBinaryWriter(fs);
            ser.WriteObject(xw, r);
            sw.Stop();
            xw.Close();
        }

        static Root DeSerialize8(Stream fs, Stopwatch sw)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            XmlDictionaryReader xr = XmlDictionaryReader.CreateBinaryReader(fs, XmlDictionaryReaderQuotas.Max);
            sw.Start();
            var obj = (Root)ser.ReadObject(xr);
            sw.Stop();
            return obj;
        }

        static void Serialize9(Root r, Stream fs, Stopwatch sw)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Root));
            sw.Start();
            ser.WriteObject(fs, r);
            sw.Stop();
        }

        static Root DeSerialize9(Stream fs, Stopwatch sw)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Root));
            sw.Start();
            var obj = (Root)ser.ReadObject(fs);
            sw.Stop();
            return obj;
        }

        static void Test(Root r, string name, Action<Root, Stream, Stopwatch> serialize,
            Func<Stream, Stopwatch, Root> deserialize)
        {
            Stopwatch sw1 = new Stopwatch();
            Stopwatch sw2 = new Stopwatch();
            double sumSer = 0.0, sumDeser = 0.0;
            long fileLen = 0;
            Stream stream = null;
            bool compareResult = false;
            int i;
            for (i = 0; i < 5; i++)
            {
                //var fs = new FileStream(String.Format(@"C:\Northwind\{0}.txt", name), FileMode.Create);
                stream = new MemoryStreamNoClose();
                sw1.Reset();
                GC.GetTotalMemory(true);
                serialize(r, stream, sw1);
                if (i != 0)
                    sumSer += sw1.Elapsed.TotalSeconds;
                //stream.Close();
            }

            fileLen = stream.Position;

            for (i = 0; i < 5; i++)
            {
                //var fs = new FileStream(String.Format(@"C:\Northwind\{0}.txt", name), FileMode.Open);
                //fileLen = fs.Length;
                sw2.Reset();
                stream.Seek(0, SeekOrigin.Begin);
                GC.GetTotalMemory(true);
                Root rr = deserialize(stream, sw2);
                if (i != 0)
                    sumDeser += sw2.Elapsed.TotalSeconds;
                //fs.Close();
                compareResult = CompareRoots(r, rr);
            }

           
            sumSer /= i;
            sumDeser /= i;
            double totalTime = sumSer + sumDeser;

            sumSer = 1.0 / sumSer;
            sumDeser = 1.0 / sumDeser;
            totalTime = 1.0 / totalTime;
            

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", name, sumSer, sumDeser, totalTime, fileLen, compareResult);
        }

        static bool CompareStringArrays(string[] a, string[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        static bool CompareDoubleArrays(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        static bool CompareRoots(Root a, Root b)
        {
            if (a.Items.Length != b.Items.Length)
                return false;

            if (a.X != b.X)
            {
                return false;
            }

            for (int i = 0; i < a.Items.Length; i++)
            {
                SerializedClassA ca = a.Items[0];
                SerializedClassA cb = b.Items[0];
                if (ca.FieldA != cb.FieldA)
                    return false;

                if (ca.FieldB != cb.FieldB)
                    return false;

                if (ca.FieldC != cb.FieldC)
                    return false;

                if (ca.FieldD != cb.FieldD)
                    return false;

                if (ca.FieldE != cb.FieldE)
                    return false;

                if (ca.FieldF != cb.FieldF)
                    return false;

                if (ca.FieldG != cb.FieldG)
                    return false;

                if (ca.FieldH != cb.FieldH)
                    return false;

                if (ca.FieldI != cb.FieldI)
                    return false;

                if (ca.FieldJ != cb.FieldJ)
                    return false;

                if (ca.FieldK != cb.FieldK)
                    return false;

                if (!CompareStringArrays(ca.FieldL, cb.FieldL))
                    return false;

                if (!CompareStringArrays(ca.FieldM, cb.FieldM))
                    return false;

                if (!CompareStringArrays(ca.FieldN, cb.FieldN))
                    return false;

                if (ca.FieldO != cb.FieldO)
                    return false;

                if (ca.FieldP != cb.FieldP)
                    return false;

                if (!CompareDoubleArrays(ca.FieldQ, cb.FieldQ))
                    return false;

                if (!CompareDoubleArrays(ca.FieldR, cb.FieldR))
                    return false;

                if (!CompareStringArrays(ca.FieldS, cb.FieldS))
                    return false;

                if (ca.FieldT.Length != cb.FieldT.Length)
                    return false;

                for (int j = 0; j < ca.FieldT.Length; j++)
                {
                    var sba = ca.FieldT[j];
                    var sbb = ca.FieldT[j];

                    if (sba.FieldA != sbb.FieldA)
                        return false;

                    if (sba.FieldB != sbb.FieldB)
                        return false;

                    if (sba.FieldC != sbb.FieldC)
                        return false;

                    if (sba.FieldD != sbb.FieldD)
                        return false;

                    if (!CompareStringArrays(sba.FieldE, sbb.FieldE))
                        return false;

                    if (!CompareStringArrays(sba.FieldF.FieldA, sbb.FieldF.FieldA))
                        return false;

                    if (!CompareStringArrays(sba.FieldG.FieldA, sbb.FieldG.FieldA))
                        return false;
                }
            }

            return true;
        }

        static void Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Root r = new Root();
            r.X = "DCS";
            r.Items = new SerializedClassA[100];

            long objCount = 0;
            for (int i = 0; i < r.Items.Length; i++)
            {
                var a = new SerializedClassA();
                objCount++;
                r.Items[i] = a;
                a.FieldA = GetRandomString();
                a.FieldB = GetRandomString();
                a.FieldC = GetRandomString();
                a.FieldD = GetRandomString();
                a.FieldE = GetRandomString();
                a.FieldF = GetRandomString();
                a.FieldG = GetRandomString();
                a.FieldH = GetRandomString();
                a.FieldI = GetRandomString();
                a.FieldJ = GetRandomString();
                a.FieldK = GetRandomString();
                a.FieldL = GetRandomStringArray();
                a.FieldM = GetRandomStringArray();
                a.FieldN = GetRandomStringArray();
                a.FieldO = Math.PI;
                a.FieldP = Math.PI;
                a.FieldQ = GetRandomDoubleArray();
                a.FieldR = GetRandomDoubleArray();
                a.FieldS = GetRandomStringArray();

                int count = _rng.Next(32) + 1;
                SerializedClassB[] items = new SerializedClassB[count];
                for (int j = 0; j < count; j++)
                {
                    var item = new SerializedClassB();
                    objCount++;
                    items[j] = item;
                    item.FieldA = GetRandomString();
                    item.FieldB = GetRandomString();
                    item.FieldC = GetRandomString();
                    item.FieldD = GetRandomString();
                    item.FieldE = GetRandomStringArray();
                    //item.FieldH = a;
                    //item.FieldI = a;
                    item.FieldF = new SerializedClassD();
                    item.FieldF.FieldA = GetRandomStringArray();
                    //item.FieldF.Hello1 = r.Items;

                    item.FieldG = new SerializedClassE();
                    item.FieldG.FieldA = GetRandomStringArray();
                    //item.FieldG.Hello2 = r.Items;
                }

                int count2 = _rng.Next(32) + 1;
                SerializedClassC[] items2 = new SerializedClassC[count2];
                for (int j = 0; j < count2; j++)
                {
                    var item = new SerializedClassC();
                    objCount++;
                    items2[j] = item;
                    item.FieldA = Math.PI;
                    item.FieldB = Math.PI;
                    item.FieldC = Math.PI;
                    item.FieldD = GetRandomString();
                    item.FieldE = GetRandomStringArray();
                    //item.FieldH = a;
                    //item.FieldI = a;
                    item.FieldF = new SerializedClassD();
                    item.FieldF.FieldA = GetRandomStringArray();
                    //item.FieldF.Hello1 = r.Items;

                    item.FieldG = new SerializedClassE();
                    item.FieldG.FieldA = GetRandomStringArray();
                    //item.FieldG.Hello2 = r.Items;
                }

                a.FieldT = items;
                a.FieldU = items2;
            }

            Test(r, "XmlSerializer", Serialize1, DeSerialize1);
            Test(r, "XmlSerializer-Bin", Serialize2, DeSerialize2);
            Test(r, "BinaryFormatter", Serialize3, DeSerialize3);
            Test(r, "SoapFormatter", Serialize4, DeSerialize4);
            Test(r, "DCS", Serialize5, DeSerialize5);
            Test(r, "DCS-Bin", Serialize6, DeSerialize6);
            Test(r, "NDCS", Serialize7, DeSerialize7);
            Test(r, "NDCS-Bin", Serialize8, DeSerialize8);
            Test(r, "DCS-Json", Serialize9, DeSerialize9);
        }
    }
}

