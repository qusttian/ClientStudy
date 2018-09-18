﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;
using System;

public class Walk : MonoBehaviour 
{
    //Socket 和缓冲区
    Socket socket;
    const int BUFFER_SIZE = 1024;
    public byte[] readBuff = new byte[BUFFER_SIZE];

    //玩家列表
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    //消息列表
    List<string> msgList = new List<string>();

    //Player预设
    public GameObject prefab;

    //自己的IP和端口
    string id;

	// Use this for initialization
	void Start () 
    {
        Connect();
        //请求其它玩家列表，略

        //把自己放在一个随机位置
        UnityEngine.Random.seed = (int)DateTime.Now.Ticks;
        float x = 100 + UnityEngine.Random.Range(-30, 30);
        float y = 0;
        float z = 100 + UnityEngine.Random.Range(-30, 30);
        Vector3 pos = new Vector3(x, y, z);
        AddPlayer(id, pos);

        //同步
        SendPos();
	}


	//连接
	private void Connect()
	{
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect("192.168.1.158", 1234);
        id = socket.LocalEndPoint.ToString();

        //recv
        socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
	}

    //接收回调
    private void ReceiveCb(IAsyncResult asyncResult)
    {
        try
        {
            int count = socket.EndReceive(asyncResult);

            //数据处理
            string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
            msgList.Add(str);

            //继续接收
            socket.BeginReceive(readBuff, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCb, null);
        }
        catch(Exception e)
        {
            socket.Close();
        }
    }



	// Update is called once per frame
	void Update () 
    {
        //处理消息列表
        for (int i = 0; i < msgList.Count;i++)
        {
            HandleMsg();

        }

        //移动
        Move();
	}


    //处理消息列表
    private void HandleMsg()
    {
        if (msgList.Count <= 0)
            return;
        string str = msgList[0];
        msgList.RemoveAt(0);

        //根据协议做不同的消息处理
        string[] args = str.Split('$');
        if(args[0]=="POS")
        {
            OnRecvPos(args[1], args[2], args[3], args[4]);
        }
        else if(args[0]=="LEAVE")
        {
            OnRecvLeave(args[1]);
        }
    }


    //处理更新位置的协议
    public void OnRecvPos(string id ,string xStr,string yStr,string zStr)
    {
        //不更新自己的位置
        if (id == this.id)
            return;
        //解析协议
        float x = float.Parse(xStr);
        float y = float.Parse(yStr);
        float z = float.Parse(zStr);
        Vector3 pos = new Vector3(x, y, z);

        //如果已经初始化该玩家
        if(players.ContainsKey(id))
        {
            players[id].transform.position = pos;
        }
        //尚未初始化该玩家
        else
        {
            AddPlayer(id, pos);
        }
    }

    //处理玩家离开的协议
    public void OnRecvLeave(string id)
    {
        if(players.ContainsKey(id))
        {
            Destroy(players[id]);
            players[id] = null;
        }
    }


    //添加玩家
    void AddPlayer(string id,Vector3 pos)
    {
        GameObject player = (GameObject)Instantiate(prefab, pos, Quaternion.identity);
        TextMesh textMesh = player.GetComponentInChildren<TextMesh>();
        textMesh.text = id;
        players.Add(id, player);
    }

    //发送位置协议
    void SendPos()
    {
        GameObject player = players[id];
        Vector3 pos = player.transform.position;

        //组装协议
        string str = "POS$";
        str += id + "$";
        str += pos.x.ToString() + "$";
        str += pos.y.ToString() + "$";
        str += pos.z.ToString();

        byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
        socket.Send(bytes);
    }

    //发送离开协议
    void SendLeave()
    {
        string str = "LEAVE$";
        str += id;
        byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
        socket.Send(bytes);
        Debug.Log("发送：" + str);
    }

	//移动
	private void Move()
	{
		if(id=="")
        {
            return;
        }
        //上移
        GameObject player = players[id];
        if(Input.GetKey(KeyCode.UpArrow))
        {
            player.transform.position += new Vector3(0, 0, 1);
            SendPos();
        }

        //下移
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            player.transform.position += new Vector3(0, 0, -1);
            SendPos();
        }

        //左移
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            player.transform.position += new Vector3(-1, 0, 0);
            SendPos();
        }

        //右移
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            player.transform.position += new Vector3(1, 0, 0);
            SendPos();
        }


	}

	private void OnDestroy()
	{
        SendLeave();
	}

}
