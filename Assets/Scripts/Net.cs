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

    //协议 
    ProtocolBase proto = new ProtocolBytes();

    //服务端的IP 和 端口
    public InputField hostInput;
    public InputField portInput;

    //用户 id,pw
    public InputField idInput;
    public InputField pwInput;
    public InputField idInputReg;
    public InputField pwInputReg;

    //按钮
    public Button btn_Connect;
    public Button btn_Login;
    public Button btn_Register;
    public Button btn_AddScore;
    public Button btn_GetScore;
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
            OnConnectToServer();
        });

        btn_Send.onClick.AddListener(delegate ()
        {
            OnClickSend();
        });
        btn_Login.onClick.AddListener(delegate ()
        {
            OnLoginClick();
        });
        btn_Register.onClick.AddListener(delegate ()
        {
            OnRegisterClick();
        });
        btn_AddScore.onClick.AddListener(delegate ()
        {
            OnAddScore();
        });
        btn_GetScore.onClick.AddListener(delegate ()
        {
            OnGetScore();
        });
    }

	//因为只有主线程能够修改UI组件的属性
	//所以在Update里更换文本
	private void Update()
	{
        recvText.text = recvStr;
	}

	public void OnConnectToServer()
    {
        //清理text
        recvStr = "";

        //Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //Connect
        hostInput.text = "127.0.0.1";
        portInput.text = "1234";
        string host = hostInput.text;
        int port = int.Parse(portInput.text);
        socket.Connect(host, port);
        clientText.text = "客户端地址："+socket.LocalEndPoint.ToString();


        //Receive 
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
    
	}
    public void OnClickSend()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString(textInput.text);
        Send(protocol);
    }

    public void OnLoginClick()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("Login");
        protocol.AddString(idInput.text);
        protocol.AddString(pwInput.text);
        Debug.Log("发送" + protocol.GetDesc());
        Send(protocol);
    }

    public void OnRegisterClick()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("Register");
        protocol.AddString(idInputReg.text);
        protocol.AddString(pwInputReg.text);
        Debug.Log("发送" + protocol.GetDesc());
        Send(protocol);
    }

    public void OnAddScore()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("AddScore");
        Debug.Log("发送" + protocol.GetDesc());
        Send(protocol);
    }

    public void OnGetScore()
    {
        ProtocolBytes protocol = new ProtocolBytes();
        protocol.AddString("GetScore");
        Debug.Log("发送" + protocol.GetDesc());
        Send(protocol);
    }


	//接收回调
    private void ReceiveCb(IAsyncResult asyncResult)
    {
        
        try
        {
            //count 是接收数据的大小
            int count = socket.EndReceive(asyncResult);
            //数据处理

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
    public void Send(ProtocolBase protocol)
    {

        byte[] bytes = protocol.Encode();
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
        if (buffCount<sizeof(Int32))
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
        ProtocolBase protocol = proto.Decode(readBuff, sizeof(Int32), msgLength);
        HandleMsg(protocol);

        //清除已处理的消息
        int count = buffCount - msgLength - sizeof(Int32);
        Array.Copy(readBuff, msgLength, readBuff, 0, count);
        buffCount = count;
        if (buffCount > 0)
        {
            ProcessData();
        }
    }

    private void HandleMsg(ProtocolBase protocolBase)
    {
        Debug.Log("客户端-> HandleMsg 开始执行！");
        if(protocolBase!=null)
        {
            Debug.Log("客户端-> HandleMsg -> protoName="+protocolBase.GetName());
        }

        ProtocolBytes proto = (ProtocolBytes)protocolBase;
        //获取数值
        int start = 0;
        string protoName = proto.GetString(start, ref start);
        int ret = proto.GetInt(start, ref start);

        //显示
        //Debug.Log("接收" + proto.GetDesc());
        recvStr = "接收" + proto.GetName() + " " + ret.ToString();
    }

    private void OnApplicationQuit()
    {
        
        Close();
        Debug.Log("应用程序退出");
    }
    public void Close()
    {
        socket.Close();
    }
}