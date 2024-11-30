﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;
using Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Server
{
    public class Server
    {
        public static Socket serverSocket;
        public static List<User> clients = new List<User>();
        public List<Room> rooms = new List<Room>();
        public Action<string> UpdateLog; // Delegate để cập nhật log trên UI
        private bool isRunning;
        private static string connectionString = "mongodb+srv://admin1:5VGZBpaXfZC15LPN@cluster0.qq9lk.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0";
        DataAccess.DatabaseHelper db = new DataAccess.DatabaseHelper(connectionString, "Datagame");

        public Server(Action<string> updateLog)
        {
            UpdateLog = updateLog;
            Thread serverThread = new Thread(StartServer) { IsBackground = true };
            serverThread.Start();
        }

        private void StartServer()
        {
            try
            {
                const string SERVER_IPADDRESS = "127.0.0.1";
                const int SERVER_PORT = 8080;
                IPAddress ipAddr = IPAddress.Parse(SERVER_IPADDRESS);
                IPEndPoint ipEP = new IPEndPoint(ipAddr, SERVER_PORT);

                //khoi tao server
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(ipEP);
                serverSocket.Listen(10);
                isRunning = true;

                UpdateLog?.Invoke("Chờ người chơi kết nối...");

                //chap nhan ket noi
                Socket clientSocket;
                while (isRunning)
                {
                    // Chờ kết nối từ client
                    clientSocket = serverSocket.Accept();
                    User client = new User(clientSocket);
                    clients.Add(client);
                    UpdateLog?.Invoke($"Đã kết nối với {clientSocket.RemoteEndPoint}");

                    // Tạo thread để xử lý dữ liệu từ client
                    Thread receivingThread = new Thread(() =>
                    {
                        try
                        {
                            // Nhận và xử lý dữ liệu từ client
                            while (true)
                            {
                                receiveData(client); // Nhận dữ liệu
                                if (!client.IsConnected) // Kiểm tra nếu client đã ngắt kết nối
                                {
                                    break; // Thoát vòng lặp nếu client ngắt kết nối
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateLog?.Invoke($"Lỗi khi nhận dữ liệu từ {clientSocket.RemoteEndPoint}: {ex.Message}");
                        }
                        finally
                        {
                            // Đảm bảo ngừng kết nối và loại bỏ client nếu có lỗi hoặc client ngắt kết nối
                            client.Stop();
                            clients.Remove(client);
                            UpdateLog?.Invoke($"Đã ngắt kết nối với {clientSocket.RemoteEndPoint}");
                        }
                    })
                    {
                        IsBackground = true // Thiết lập thread nền
                    };
                    receivingThread.Start();
                }
            }
            catch (Exception ex)
            {
                UpdateLog?.Invoke("Lỗi khi khởi tạo server: " + ex.Message);
            }
        }

        private void receiveData(User client)
        {
            try
            {
                // Tạo buffer để lưu dữ liệu nhận được
                byte[] buffer = new byte[1024];
                int bytesReceived = client.UserSocket.Receive(buffer);

                if (bytesReceived == 0)
                {
                    // Client đã ngắt kết nối
                    UpdateLog?.Invoke($"{client.UserSocket.RemoteEndPoint} đã ngắt kết nối");
                }

                // Chuyển đổi byte thành chuỗi (sử dụng UTF-8 encoding)
                string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                UpdateLog?.Invoke($"Dữ liệu nhận được từ {client.UserSocket.RemoteEndPoint}: {message}");

                Packet packet = ParsePacket(client, message);
                analyzingPacket(client, packet);
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Socket exception: " + ex.Message);
                client.Stop();
                clients.Remove(client);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void sendData(User client, Packet packet)
        {
            try
            {
                // Chuẩn bị dữ liệu để gửi
                byte[] msgBytes = packet.ToBytes();

                // Gửi dữ liệu qua socket
                client.UserSocket.Send(msgBytes);

                UpdateLog?.Invoke($"Đã gửi dữ liệu cho {client.UserSocket.RemoteEndPoint} {packet.Payload}");
            }
            catch (SocketException ex)
            {
                UpdateLog?.Invoke("Socket exception: " + ex.Message);
                // Xử lý client bị mất kết nối
                client.Stop();
                clients.Remove(client);
            }
        }

        private Packet ParsePacket(User client, string msg)
        {
            string[] payload = msg.Split(';');
            if (payload.Length == 0)
            {
                return null; // Không có dữ liệu
            }

            // Lấy phần còn lại của msg, bỏ payload[0] (command)
            string remainingMsg = string.Join(";", payload.Skip(1));

            // Xác định loại packet dựa trên phần tử đầu tiên
            PacketType packetType;
            if (Enum.TryParse(payload[0], out packetType))
            {
                switch (packetType)
                {
                    case PacketType.LOGIN:
                        return new LoginPacket(remainingMsg);
                    case PacketType.REGISTER:
                        return new RegisterPacket(remainingMsg);
                    case PacketType.LOGOUT:
                        return new LogoutPacket(remainingMsg);
                    case PacketType.CREATE_ROOM:
                        return new CreateRoomPacket(remainingMsg);
                    case PacketType.JOIN_ROOM:
                        return new JoinRoomPacket(remainingMsg);
                    case PacketType.LEAVE_ROOM:
                        return new LeaveRoomPacket(remainingMsg);
                    case PacketType.START:
                        return new StartPacket(remainingMsg);
                    case PacketType.DESCRIBE:
                        return new DescribePacket(remainingMsg);
                    case PacketType.GUESS:
                        return new GuessPacket(remainingMsg);
                    default:
                        return null; // Không biết loại packet
                }
            }
            return null; 
        }

        private string GenerateRoomId()
        {
            int length = 4; // Độ dài mã phòng
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Các ký tự cho mã phòng
            Random random = new Random();
            char[] RoomID = new char[length];

            for (int i = 0; i < length; i++)
            {
                RoomID[i] = chars[random.Next(chars.Length)];
            }

            return new string(RoomID);
        }


        // xu ly sau khi nhan du lieu tu client
        private void analyzingPacket(User client, Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.LOGIN:
                    LoginPacket loginPacket = (LoginPacket)packet;
                    if (db.LoginUser(loginPacket.Username, loginPacket.Password))
                    {
                        LoginResultPacket result = new LoginResultPacket("success");
                        sendData(client, result);
                        UpdateLog.Invoke($"{loginPacket.Username} đăng nhập thành công");
                        client.Name = loginPacket.Username;
                    }
                    else
                    {
                        LoginResultPacket result = new LoginResultPacket("fail");
                        sendData(client, result);
                        UpdateLog.Invoke($"{loginPacket.Username} đăng nhập thất bại");
                    }
                    break;
                case PacketType.REGISTER:
                    RegisterPacket registerPacket = (RegisterPacket)packet;
                    if (db.RegisterUser(registerPacket.Username, registerPacket.Email, registerPacket.Password))
                    {
                        RegisterResultPacket result = new RegisterResultPacket("success");
                        sendData(client, result);
                        UpdateLog.Invoke($"{registerPacket.Username} đăng ký tài khoản thành công");
                    }
                    else
                    {
                        RegisterResultPacket result = new RegisterResultPacket("fail");
                        sendData(client, result);
                        UpdateLog.Invoke($"{registerPacket.Username} đăng ký tài khoản thất bại");
                    }
                    break;
                case PacketType.LOGOUT:
                    LogoutPacket logoutPacket = (LogoutPacket)packet;
                    UpdateLog.Invoke($"{logoutPacket.Username} đã đăng xuất");
                    break;
                case PacketType.CREATE_ROOM:
                    CreateRoomPacket createRoomPacket = (CreateRoomPacket)packet;

                    User host = client;
                    client.Name = createRoomPacket.username;

                    // Tạo phòng mới
                    string roomId = GenerateRoomId();
                    int maxPlayers = createRoomPacket.Max_player;

                    Room room = new Room(roomId, maxPlayers);
                    room.host = host;
                    room.players.Add(host);
                    room.status = "waiting";

                    rooms.Add(room);

                    RoomInfoPacket roomInfoPacket = new RoomInfoPacket($"{roomId};{client.Name};{room.status};{maxPlayers};{room.players.Count};0");
                    sendData(client, roomInfoPacket);
                    UpdateLog.Invoke($"{client.Name} đã tạo phòng {roomId}");
                    break;
                case PacketType.JOIN_ROOM:
                    JoinRoomPacket joinRoomPacket = (JoinRoomPacket)packet;
                    string roomIdToJoin = joinRoomPacket.RoomId;
                    string ussername = joinRoomPacket.Username;

                    room = rooms.FirstOrDefault(r => r.RoomId == roomIdToJoin);
                    string join_result = "SUCCESS";

                    // Kiểm tra xem phòng có tồn tại không
                    if (room == null)
                    {
                        join_result = "NOT_EXIST";
                    }
                    // kiểm tra phòng đang chơi
                    if (room.status == "playing" || room.status == "finished")
                    {
                        join_result = "PLAYING";
                    }
                    // kiểm tra phòng đầy
                    if (room.players.Count == room.maxPlayers)
                    {
                        join_result = "FULL";
                    }

                    if (join_result=="SUCCESS")
                    {
                        room.players.Add(client);
                        roomInfoPacket = new RoomInfoPacket($"{roomIdToJoin};{room.host.Name};{room.status};{room.maxPlayers};{room.players.Count};0");
                        sendData(client, roomInfoPacket);
                        UpdateLog.Invoke($"{client.Name} đã tham gia phòng {roomIdToJoin}");
                    }

                    break;
                case PacketType.LEAVE_ROOM:
                    break;
                case PacketType.START:
                    break;
                case PacketType.DESCRIBE:
                    break;
                case PacketType.GUESS:
                    break;
                default:
                    break;
            }
        }




        public void StopServer()
        {
            foreach (var client in clients)
            {
                client.Stop();
            }
            serverSocket.Close();
            serverSocket = null;
        }

    }
}
