using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive: MonoBehaviour
{
    Thread receiveThread;
    UdpClient udpClient;
    public int port = 5052;
    public bool printToConsole = false;

    private volatile bool startReceiving = true;
    public string receivedData;

    public void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    public void Update()
    {
        // Update logic can be added here if needed
        if (!String.IsNullOrEmpty(receivedData))
        {
            // Process the received data here
            if (printToConsole)
            {
                 print(receivedData);
            }

        }
    }
    private void ReceiveData()
    {
        udpClient = new UdpClient(port);
        while (startReceiving)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpClient.Receive(ref remoteEndPoint);
                string receivedString = Encoding.UTF8.GetString(receivedData);
                this.receivedData = receivedString;
            }
            catch (SocketException)
            {
                
            }
        }
    }

    public void OnApplicationQuit()
    {
        startReceiving = false;
        udpClient?.Close();
        receiveThread?.Join();
    }
}






