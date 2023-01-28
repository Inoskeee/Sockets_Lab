using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Messaging;

namespace Sockets
{
    public partial class frmMain : Form
    {
        private UdpClient Listener;                   // сокет сервера
        private List<Thread> Threads = new List<Thread>();      // список потоков приложения (кроме родительского)
        private List<IPEndPoint> EndPoints = new List<IPEndPoint>();
        private bool _continue = true;                          // флаг, указывающий продолжается ли работа с сокетами

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());    // информация об IP-адресах и имени машины, на которой запущено приложение
            IPAddress IP = hostEntry.AddressList[0];                        // IP-адрес, который будет указан при создании сокета
            int Port = 1010;                                                // порт, который будет указан при создании сокета

            // определяем IP-адрес машины в формате IPv4
            foreach (IPAddress address in hostEntry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP = address;
                    break;
                }

            // вывод IP-адреса машины и номера порта в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + IP.ToString() + "  :  " + Port.ToString();

            Listener = new UdpClient(5555);

            // создаем и запускаем поток, выполняющий обслуживание серверного сокета
            Threads.Clear();
            Threads.Add(new Thread(ReceiveMessage));
            Threads[Threads.Count-1].Start();
        }

        // работа с клиентскими сокетами
        private void ReceiveMessage()
        {
            // входим в бесконечный цикл для работы с клиентскими сокетом
            while (_continue)
            {
                try
                {
                    var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    var data = Listener.Receive(ref remoteEP);
                    if (!EndPoints.Contains(remoteEP))
                    {
                        EndPoints.Add(remoteEP);
                    }
                    var message = Encoding.UTF8.GetString(data);
                    message = message.Replace("\0", "");

                    if (message != "newConnection")
                    {
                        rtbMessages.Invoke((MethodInvoker)delegate
                        {
                            if (message.Replace("\0", "") != "")
                                rtbMessages.Text += "\n >> " + message;             // выводим полученное сообщение на форму
                        });
                    }
                    else
                    {
                        rtbMessages.Invoke((MethodInvoker)delegate
                        {
                            message = rtbMessages.Text;
                        });
                    }

                    SendAllClients(message);
                }
                catch(Exception ex)
                {

                }
            }
        }

        private void SendAllClients(string msg)
        {
            foreach(var client in EndPoints)
            {
                try
                {
                    byte[] buff = Encoding.UTF8.GetBytes(msg);
                    int bytes = Listener.Send(buff, buff.Length, client);
                }
                catch (Exception ex) { }
            }
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с сокетами завершена
            
            // завершаем все потоки
            foreach (Thread t in Threads)
            {
                t.Abort();
                t.Join(500);
            }

            // приостанавливаем "прослушивание" серверного сокета
            if (Listener != null)
                Listener.Close();
        }
    }
}