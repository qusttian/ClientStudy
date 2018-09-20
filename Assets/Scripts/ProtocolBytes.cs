using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//字节流协议模型
class ProtocolBytes:ProtocolBase
    {
        //传输的字节流，整个协议都用byte数组表达
        public byte[] bytes;

        //解码器，将字节流转换成ProtocolBytes对象
        public override ProtocolBase Decode(byte[] readBuff,int start,int length)
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.bytes = new byte[length];
            Array.Copy(readBuff, start, protocol.bytes, 0, length);
            return protocol;
        }

        //编码器，返回字节流
        public override byte[] Encode()
        {
            return bytes;
        }

        //协议名称，获取协议的第一个字符串
        public override string GetName()
        {
            return GetString(0);
        }

        //获取协议的描述
        public override string GetDesc()
        {
            string str = "";
            if (bytes == null) return str;
            for(int i=0;i<bytes.Length;i++)
            {
                int b = (int)bytes[i];
                str += b.ToString() + " ";
            }
            return str;
        }

        //添加字符串,拼装字符串，先是表示字符串大小的字节，再是字符串本身
        public void AddString(string str)
        {
            Int32 len = str.Length;
            byte[] lenBytes = BitConverter.GetBytes(len);
            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(str);
            if(bytes==null)
            {
                bytes = lenBytes.Concat(strBytes).ToArray();
            }
            else
            {
                bytes = bytes.Concat(lenBytes).Concat(strBytes).ToArray();
            }
        }

        //添加整型，将Int 类型的整数转换成byte数组
        //判断bytes是否为空，如果不为空，则在原数组后面附加数据
        public void AddInt(int num)
        {
            byte[] numBytes = BitConverter.GetBytes(num);
            if(bytes==null)
            {
                bytes = numBytes;
            }
            else
            {
                bytes = bytes.Concat(numBytes).ToArray();
            }
        }

        //判读一些特殊情况，比如数组长度不足4，无法读取数值
        //这时用默认值取代无法读取的数据
        //BitConverter.ToInt32实现将byte数组start位置后的4个字节转换成int数据
        public int GetInt(int start,ref int end)
        {
            if (bytes == null) return 0;
            if (bytes.Length < start + sizeof(Int32)) return 0;
            end = start + sizeof(Int32);
            return BitConverter.ToInt32(bytes, start);
        }

        public int GetInt(int start)
        {
            int end = 0;
            return GetInt(start, ref end);
        }

        //将float类型的浮点数转换成byte数组
        public void AddFloat(float num)
        {
            byte[] numBytes = BitConverter.GetBytes(num);
            if(bytes==null)
            {
                bytes = numBytes;
            }
            else
            {
                bytes = bytes.Concat(numBytes).ToArray();
            }
        }

        //将byte数组转换成浮点数
        //BitConverter.ToSingle实现将byte数组star位置后的8个字节转换成float数据
        public float GetFloat(int start,ref int end)
        {
            if (bytes == null) return 0;
            if (bytes.Length < start + sizeof(float)) return 0;
            end = start + sizeof(float);
            return BitConverter.ToSingle(bytes, start);
        }

        public float GetFloat(int start)
        {
            int end = 0;
            return GetFloat(start, ref end);
        }


        //从字节数组的start处开始读取字符串
        //如果start后的字节数小于4，则不能读取字符串的大小
        //如果字节数小于字符串长度，那么一定是出错了
        //BitConverter.ToInt32实现将byte数组start位置后的4个字节转换成int数据
        public string GetString(int start,ref int end)
        {
            if (bytes == null) return "GetString Null";
            if (bytes.Length < start + sizeof(Int32)) return "bytes.Length < start + sizeof(Int32)";
            Int32 strLen = BitConverter.ToInt32(bytes, start);
            if (bytes.Length < start + sizeof(Int32) + strLen) return "bytes.Length < start + sizeof(Int32) + strLen";
            string str = System.Text.Encoding.UTF8.GetString(bytes, start + sizeof(Int32), strLen);
            end = start + sizeof(Int32) + strLen;
            return str;
        }

        //对上述方法的封装，使得调用时可以忽略end参数
        public string GetString(int start)
        {
            int end = 0;
            return GetString(start, ref end);
        }  

    }

