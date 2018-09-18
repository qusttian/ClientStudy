using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;

public class Net : MonoBehaviour 
{
    //与服务端的套接字
    Socket socket;

    int buffCount = 0;
    byte[] lenBytes = new byte[sizeof(UInt32)];
    Int32 msgLength = 0;

    //服务端的IP 和 端口
    public InputField hostInput;
    public InputField portInput;

    //连接按钮 和 发送按钮
    public Button btn_Connect;
    public Button btn_Send;

    //显示客户端收到的消息
    public Text recvText;
    private string recvStr;

    //显示客户端IP和端口
    public Text clientText;

    //聊天输入框
    public InputField textInput;


    //接收缓冲器
    public const int BUFFER_SIZE = 1024;
    public byte[] readBuff = new byte[BUFFER_SIZE];


	private void Start()
	{
        btn_Connect.onClick.AddListener(delegate ()
        {
            MyConnectToServer();
        });

        btn_Send.onClick.AddListener(delegate ()
        {

            MySendToServer();
        });
	}

	//因为只有主线程能够修改UI组件的属性
	//所以在Update里更换文本
	private void Update()
	{
        recvText.text = recvStr;
	}

	public void MyConnectToServer()
    {
        //清理text
        recvStr = "";

        //Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //Connect
        string host = hostInput.text;
        int port = int.Parse(portInput.text);
        socket.Connect(host, port);
        clientText.text = "客户端地址："+socket.LocalEndPoint.ToString();


        //Receive 
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
    
	}

	//接收回调
    private void ReceiveCb(IAsyncResult asyncResult)
    {
        try
        {
            //count 是接收数据的大小
            int count = socket.EndReceive(asyncResult);
            ////数据处理
            //string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
            //if (recvStr.Length > 300)
            //{
            //    recvStr = "";
            //}
            //recvStr += str+"\n";

            buffCount += count;
            ProcessData();

            //继续接收
            socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
        }
        catch(Exception e)
        {
            recvStr = "连接已断开";
            socket.Close();
        }
    }

    //发送数据
    public void MySendToServer()
    {
        string str = textInput.text;
        byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
        byte[] length = BitConverter.GetBytes(bytes.Length);
        byte[] sendBuff = length.Concat(bytes).ToArray();
        try
        {
            socket.Send(sendBuff);
        }
        catch(Exception e)
        {
            Debug.Log("客户端数据发送异常：" + e.Message);
        }
    }

    private void ProcessData()
    {
        //小于长度字节
        if(buffCount<sizeof(Int32))
        {
            return;
        }

        //消息长度
        Array.Copy(readBuff, lenBytes, sizeof(Int32));
        msgLength = BitConverter.ToInt32(lenBytes, 0);
        if(buffCount<msgLength+sizeof(Int32))
        {
            return;
        }

        //消息处理
        string str = System.Text.Encoding.UTF8.GetString(readBuff, sizeof(Int32), (int)msgLength);
        recvStr = str;

        //清除已处理的消息
        int count = buffCount - msgLength - sizeof(Int32);
        Array.Copy(readBuff, msgLength, readBuff, 0, count);

        buffCount = count;
        if(buffCount>0)
        {
            ProcessData();
        }


    }
}