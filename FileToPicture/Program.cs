using FastBitmapLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileToPicture
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!File.Exists("input") && args.Length == 0)
            {
                Console.WriteLine("input 파일이 존재하지 않으며 드래그된 파일도 없습니다");
                Console.Read();
                Environment.Exit(0);
            }
            string filename = null;
            if(File.Exists("input") && args.Length == 1)
            {
                Console.WriteLine("input 파일과 드래그된 파일이 두개나 있습니다!");
                Console.WriteLine("1.input 파일 사용");
                Console.WriteLine("2.드래그된 파일사용");
                if(Console.ReadLine() == "1")
                {
                    filename = "input";
                }
                else
                {
                    filename = args[0];
                }
            }
            else
            {
                filename = File.Exists("input") ? "input" : args[0];
            }
            Console.WriteLine("디코딩 하시겠습니까? Y/N");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                if (args.Length == 1)
                {
                    File.WriteAllBytes("Decode.output", Decode(args[0]));
                }
                else
                {
                    File.WriteAllBytes("Decode.output", Decode("input"));
                }
                Console.WriteLine("디코딩 완료");
                Console.Read();
                Environment.Exit(0);
            }

            long length = new FileInfo(filename).Length; 
        
            Task.Run(async () =>
            {
                await WantThreadAsync(filename,length);
            }).GetAwaiter().GetResult();

            Console.WriteLine("검증 하시겠습니까? (램용량에 주의) Y/N");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                Validate(filename);
            }
            Console.WriteLine("완료");
            Console.Read();
        }

        private static async Task WantThreadAsync(string filename,long filelength)
        {
            Stopwatch stopWatch = new Stopwatch();
            Console.WriteLine("1.싱글쓰레드");
            Console.WriteLine("2.멀티쓰레드");
            if (Console.ReadLine() == "1")
            {
                Console.Write("모양을 선택해주세요. (1 : 정사각형, 2 : 직사각형, 3 : 원) : ");
                PictureSize size = CalculateSize(filelength, (PictureShape)(Convert.ToInt32(Console.ReadLine()) - 1));
                stopWatch.Start();
                Run(filename, filelength, size).Save("output.png", ImageFormat.Png);
            }
            else
            {

                byte[] bytes = File.ReadAllBytes(filename);
                re:
                int threadCount = 0;
                int EachSize = 0;
                Console.WriteLine("멀티 코어로 하시겠습니까? Y/N");
                if (Console.ReadLine().ToUpper() == "Y")
                {
                    Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                    foreach (ProcessThread processThread in currentProcess.Threads)
                    {
                        processThread.ProcessorAffinity = currentProcess.ProcessorAffinity;
                    }
                    threadCount = currentProcess.Threads.Count;
                    EachSize = (int)Math.Ceiling((double)bytes.Length / (double)threadCount);
                }
                else
                {
                    Console.WriteLine("원하는 쓰레드 수를 입력하세요");
                    threadCount = Int32.Parse(Console.ReadLine());
                    EachSize = (int)Math.Ceiling((double)bytes.Length / (double)threadCount);
                    if (bytes.Length / threadCount == 0)
                    {
                        Console.WriteLine("쓰레드가 너무많습니다");
                        goto re;
                    }
                }
                stopWatch.Start();
                List<byte[]> EachByte = new List<byte[]>();
                int locationbyte = 0;
                for (int i = 0; i != threadCount; i++)
                {
                    if(locationbyte >= bytes.Length)
                    {
                        break;
                    }
                    if (i == threadCount - 1)
                    {
                        EachByte.Add(bytes.Skip(locationbyte).Take(bytes.Length - locationbyte).ToArray());
                    }
                    else
                    {
                        EachByte.Add(bytes.Skip(locationbyte).Take(EachSize).ToArray());
                        locationbyte += EachSize;
                    }
                }
                List<Color[]> colors = new List<Color[]>(threadCount);
                PictureSize size = CalculateSize(bytes.Length, PictureShape.Square);
                double length = (double)bytes.Length / 3;
                length /= threadCount;
                if ((Math.Sqrt(length) % 1) != 0) //소수 나옴
                {
                    int Picsize = (int)Math.Ceiling(Math.Sqrt(length));
                    size = new PictureSize(Picsize, Picsize);
                }
                else
                {
                    int Picsize = (int)Math.Sqrt(length);
                    size = new PictureSize(Picsize, Picsize);
                }

                Task<Color[]>[] tasks = new Task<Color[]>[threadCount];// 띠용?????
                for (int index = 0; index != threadCount; index++)
                {
                    int i = index;
                    tasks[i] = Task<Color[]>.Factory.StartNew(() => { return ColorsRun(EachByte[i], size); });
                }
                await Task.WhenAll(tasks);
                foreach(Task<Color[]> task in tasks)
                {
                    colors.Add(task.Result);
                }
                Bitmap Process = MergeImages(colors);
                Process.Save("output.png", ImageFormat.Png);
            }
            stopWatch.Stop();
            Console.WriteLine("걸린시간 " + stopWatch.Elapsed);
            GC.Collect();
        }

        static private Bitmap MergeImages(List<Color[]> colors)
        {
            int length = colors[0].Length * (colors.Count-1) + colors.Last().Length;
            PictureSize size;
            int Picsize = (int)Math.Ceiling(Math.Sqrt(length));
            size = new PictureSize(Picsize, Picsize);
            Bitmap bit = new Bitmap(size.W, size.H);
            int index = 0;
            int offset = 0;
            int fulloffset = 0;
            using (var fastBitmap = bit.FastLock())
            {
                for (int x = 0; x != size.W; x++)
                {
                    for (int y = 0; y != size.H; y++)
                    {
                        if (colors[index].Length == offset)
                        {
                            offset = 0;
                            index++;
                            if (index == colors.Count)
                            {
                                goto exit;
                            }
                        }

                        fastBitmap.SetPixel(x, y, colors[index][offset]);
                        offset++;
                        fulloffset++;
                    }

                    if (fulloffset == length)
                    {
                        goto exit;
                    }
                }
            }
            exit:
            return bit;
        }


        static Color[] ColorsRun(byte[] bytes, PictureSize size)
        {
            Color[] ColorMap = new Color[(int)Math.Ceiling((double)bytes.Length / 3)];
            int offset = 0;
            int index = 0;
            int x = 0;
            int y = 0;
            for (int i = 0; i != bytes.Length; i++)
            {
                if (offset + 2 == bytes.Length)
                {
                    ColorMap[index] = Color.FromArgb(2, bytes[offset], bytes[offset + 1], 0);
                    goto Exit;
                }
                else if (offset + 1 == bytes.Length)
                {
                    ColorMap[index] = Color.FromArgb(1, bytes[offset], 0, 0);
                }
                else if (bytes.Length <= offset)
                {
                    goto Exit;
                }
                else
                {
                    ColorMap[index] = Color.FromArgb(255, bytes[offset], bytes[offset + 1], bytes[offset + 2]);
                }
                offset += 3;
                index++;

                if (y == size.W - 1)
                {
                    y = 0;
                    x++;
                }
                else
                {
                    y++;
                }
            }
            Exit:
            return ColorMap;
        }

        static Bitmap Run(string filename, long Length, PictureSize size)
        {
            Bitmap Process = new Bitmap(size.W, size.H);
            int offset = 0;
            byte[] bytes = new byte[3];
            FileStream stream = new FileStream(filename, FileMode.Open);
           long last = size.W * size.H * 3 + (Length - size.W * size.H * 3);
            using (var fastBitmap = Process.FastLock())
            {
                for (int x = 0; x != Process.Width; x++)
                {
                    for (int y = 0; y != Process.Height; y++)
                    {
                        if (!size.IsCircle)
                        {
                            stream.Read(bytes, 0, 3);
                            if (offset >= last)
                            {
                                fastBitmap.SetPixel(x, y, Color.FromArgb(3, 0, 0, 0));
                                break;
                            }
                            else if (offset + 2 == Length)
                            {
                                fastBitmap.SetPixel(x, y, Color.FromArgb(2, bytes[0], bytes[1], 0));
                                goto Exit;
                            }
                            else if (offset + 1 == Length)
                            {
                                fastBitmap.SetPixel(x, y, Color.FromArgb(1, bytes[0], 0, 0));
                            }
                            else if (Length <= offset)
                            {
                                goto Exit;
                            }
                            else
                            {
                                fastBitmap.SetPixel(x, y, Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
                            }
                            offset += 3;
                        }
                        else
                        {
                            if (Length <= offset)
                            {
                                goto Exit;
                            }
                            else if (size.W * size.W / 4.0 > (x - size.W / 2.0) * (x - size.W / 2.0) + (y - size.H / 2.0) * (y - size.H / 2.0)) //그리기
                            {
                                stream.Read(bytes, 0, 3);
                                if (offset + 2 == Length)
                                {
                                    fastBitmap.SetPixel(x, y, Color.FromArgb(2, bytes[0], bytes[1], 0));
                                    goto Exit;
                                }
                                else if (offset + 1 == Length)
                                {
                                    fastBitmap.SetPixel(x, y, Color.FromArgb(1, bytes[0], 0, 0));
                                }
                                else
                                {
                                    fastBitmap.SetPixel(x, y, Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
                                }
                                offset += 3;
                            }
                            else
                            {
                                fastBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                            }
                        }
                    }
                }
            }
            Exit:
            return Process;
        }

        private static PictureSize CalculateSize(long length, PictureShape shape)
        {
            if (length > 1610546700) { throw new Exception("파일이 1.6GB보다 큽니다."); }
            if (shape == PictureShape.Square)
            {
                length /= 3;
                if ((Math.Sqrt(length) % 1) != 0) //소수 나옴
                {
                    int size = (int)Math.Ceiling(Math.Sqrt(length));
                    return new PictureSize(size, size);
                }
                else
                {
                    int size = (int)Math.Sqrt(length);
                    return new PictureSize(size, size);
                }
            }
            else if (shape == PictureShape.Rectangular)
            {
                int size = 0;
                if (((double)length / (double)3) % 1 != 0) //소수 나옴
                {
                    size = Convert.ToInt32(length);
                    size /= 3;
                    size += 1;
                }
                else
                {
                    size = Convert.ToInt32(length);
                    size /= 3;
                }
                List<Divisor> divisors = GetDivisors(size);
                for (int i = 0; i != divisors.Count; i++)
                {
                    Console.WriteLine(i + " : w:" + divisors[i].A + " h:" + divisors[i].B);
                }
                int input = Convert.ToInt32(Console.ReadLine());
                return new PictureSize(divisors[input].A, divisors[input].B);
            }
            else
            {
                length /= 3;
                int size = (int)Math.Ceiling(0.56419 * Math.Sqrt(length));
                size *= 2;
                return new PictureSize(size, size, true);
            }
        }

        private static List<Divisor> GetDivisors(int num)
        {
            List<Divisor> temp = new List<Divisor>();
            for (int i = 1; i <= num; i++)
            {
                if (num % i == 0)
                {
                    temp.Add(new Divisor(i, num / i));
                }
            }
            return temp;
        }

        private static void Validate(string input)
        {
            byte[] bytes = File.ReadAllBytes(input);
            byte[] data = Decode("output.png");
            if (bytes.Length == data.Length)
            {
                Console.WriteLine("검증통과/bytes 사이즈");
            }
            else
            {
                Console.WriteLine("검증실패/bytes 사이즈");
            }
            if (bytes.SequenceEqual(data))
            {
                Console.WriteLine("검증통과/파일손상 없음");
            }
            else
            {
                Console.WriteLine("검증실패/파일손상 있음");
            }
            System.Diagnostics.Process.Start("output.png");
            Console.WriteLine("Validate.output 쓰는중..");
            File.WriteAllBytes("Validate.output", data);
            Console.WriteLine("Validate.output 쓰기 완료");
        }

        private static byte[] Decode(string input)
        {
            List<byte> data = new List<byte>();
            Image image = Bitmap.FromFile(input);
            Bitmap myBitmap = (Bitmap)image;
            using (var fastBitmap = myBitmap.FastLock())
            {
                for (int x = 0; x != myBitmap.Width; x++)
                {
                    for (int y = 0; y != myBitmap.Height; y++)
                    {
                        Color pixel = fastBitmap.GetPixel(x, y);
                        if (pixel.A == 1)
                        {
                            data.Add(pixel.R);
                        }
                        else if (pixel.A == 2)
                        {
                            data.Add(pixel.R);
                            data.Add(pixel.G);
                        }
                        else if (pixel.A == 3)
                        {
                            continue;
                        }
                        else if (pixel != Color.FromArgb(0, 0, 0, 0))
                        {
                            data.Add(pixel.R);
                            data.Add(pixel.G);
                            data.Add(pixel.B);
                        }
                    }
                }
            }
            return data.ToArray();
        }
    }

    internal class PictureSize
    {
        public int W;
        public int H;
        public bool IsCircle;

        public PictureSize(int w, int h)
        {
            W = w;
            H = h;
        }

        public PictureSize(int w, int h, bool isCircle) : this(w, h)
        {
            IsCircle = isCircle;
        }
    }

    internal class Divisor
    {
        public int A;
        public int B;

        public Divisor(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    internal enum PictureShape
    {
        Square,
        Rectangular,
        Circle
    }
}
