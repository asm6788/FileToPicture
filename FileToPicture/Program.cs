﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileToPicture
{
    class Program
    {

        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("input");
            PictureSize size = DecideSize(bytes.Length);
            Bitmap Process = new Bitmap(size.W, size.H);
            int offset = 0;
            for (int x = 0; x != Process.Width; x++)
            {
                for (int y = 0; y != Process.Height; y++)
                {
                    if (!size.IsCircle)
                    {
                        if (offset + 2 == bytes.Length)
                        {
                            Process.SetPixel(x, y, Color.FromArgb(1, bytes[offset], bytes[offset + 1],0));

                        }
                        else if(bytes.Length <= offset)
                        {
                            goto Exit;
                        }
                        else
                        {
                            Process.SetPixel(x, y, Color.FromArgb(255, bytes[offset], bytes[offset + 1], bytes[offset + 2]));
                        }
                        offset += 3;
                    }
                    else
                    {
                        if (offset + 2 == bytes.Length)
                        {
                            Process.SetPixel(x, y, Color.FromArgb(1, bytes[offset], bytes[offset + 1], 0));
                            offset += 3;
                        }
                        else if (bytes.Length <= offset)
                        {
                            goto Exit;
                        }
                        else if (size.W * size.W / 4.0 > (x - size.W / 2.0) * (x - size.W / 2.0) + (y - size.H / 2.0) * (y - size.H / 2.0)) //그리기
                        {
                            Process.SetPixel(x, y, Color.FromArgb(255, bytes[offset], bytes[offset + 1], bytes[offset + 2]));
                            offset += 3;
                        }
                        else
                        {
                            Process.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                        }
                    }
                }
            }
            Exit:
            if(offset < bytes.Length/3)
            {
                Console.WriteLine("생성실패! 너무 사진 사이즈가 작습니다");
            }
            Process.Save("output.png", ImageFormat.Png);
            Process.Dispose();
            Validate();
            Console.Read();
        }

 
        static PictureSize DecideSize(int len)
        {
            Console.WriteLine("1.정사각형");
            Console.WriteLine("2.직사각형");
            Console.WriteLine("3.원");
            string select = Console.ReadLine();
            if (select == "1")
            {
                len /= 3;
                if ((Math.Sqrt(len) % 1) != 0) //소수 나옴
                {
                    int size = (int)Math.Ceiling(Math.Sqrt(len));
                    return new PictureSize(size, size);
                }
                else
                {
                    int size = (int)Math.Sqrt(len);
                    return new PictureSize(size, size);
                }
            }
            else if (select == "2")
            {
                int size = 0;
                if (((double)len / (double)3) % 1 != 0) //소수 나옴
                {
                    size = Convert.ToInt32(len);
                    size /= 3;
                    size += 1;
                }
                else
                {
                    size = Convert.ToInt32(len);
                    size /= 3;
                }
                List<Divisor> divisors = getDivisors(size);
                for (int i =0; i != divisors.Count;i++)
                {
                    Console.WriteLine(i+" : w:"+ divisors[i].A + " h:" + divisors[i].B);
                }
                int input = Convert.ToInt32(Console.ReadLine());
                return new PictureSize(divisors[input].A, divisors[input].B);
            }
            else
            {
                len /= 3;
                int size = (int)Math.Ceiling(0.56419 * Math.Sqrt(len));
                size *= 2;
                return new PictureSize(size, size,true);
            }
        }

        static List<Divisor> getDivisors(int num)
        {
            List<Divisor> temp = new List<Divisor>();
            for(int i = 1; i <= num;i++)
            {
                if (num % i == 0)
                {
                    temp.Add(new Divisor(i, num / i));
                }
            }
            return temp;
        }
        static void Validate()
        {
            List<byte> data = new List<byte>();
            Image image = Bitmap.FromFile("output.png");
            Bitmap myBitmap = (Bitmap)image;
            byte[] bytes = File.ReadAllBytes("input");
            for (int x = 0; x != myBitmap.Width; x++)
            {
                for (int y = 0; y != myBitmap.Height; y++)
                {
                    Color pixel = myBitmap.GetPixel(x, y);
                    if (pixel.A == 1)
                    {
                        data.Add(pixel.R);
                        data.Add(pixel.G);
                    }
                    else if (pixel != Color.FromArgb(0, 0, 0, 0))
                    {
                        data.Add(pixel.R);
                        data.Add(pixel.G);
                        data.Add(pixel.B);
                    }
                   
                }
            }
            if (File.ReadAllBytes("input").Length == data.ToArray().Length)
            {
                Console.WriteLine("검증통과");
            }
            else
            {
                Console.WriteLine("검증실패");
            }
            System.Diagnostics.Process.Start("output.png");
            File.WriteAllBytes("Validate.exe", data.ToArray());
            System.Diagnostics.Process.Start("Validate.exe");
            Console.Read();
        }
    }
    
    class PictureSize
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

    class Divisor
    {
        public int A;
        public int B;

        public Divisor(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}
