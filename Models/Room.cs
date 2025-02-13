﻿using Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Timers;

namespace Models
{
    public class Room
    {
        // Thuộc tính
        public string RoomId; // ID của phòng gồm 4 ký tự số và chữ in hoa
        public List<User> players;  // Danh sách người chơi trong phòng
        public string currentKeyword; // Từ khóa hiện tại
        public int roundTime = 60; // Thời gian của mỗi vòng chơi (tính bằng giây)
        public System.Timers.Timer roundTimer; // Timer đếm ngược cho mỗi vòng chơi
        public string status; // waiting, playing, finished
        public bool IsGameStarted = false; // Trạng thái vòng chơi (đang diễn ra hay không)
        public int maxPlayers; // Số lượng người chơi tối đa trong phòng
        public Random random;
        public string host;

        public User currentDrawer; // Người chơi hiện đang vẽ
        public int currentDrawerIndex; // Vị trí của người vẽ hiện tại trong danh sách người chơi


        public Room(string RoomId, string host, int maxPlayers, User player)
        {
            players = new List<User>();
            random = new Random();
            currentDrawerIndex = -1;
            this.RoomId = RoomId;
            this.maxPlayers = maxPlayers;
            this.status = "WAITING";
            this.host = host;
            this.currentKeyword = "";
            currentDrawer = player;

            // Cài đặt bộ đếm thời gian cho vòng chơi (60 giây)
            roundTimer = new System.Timers.Timer(roundTime * 1000);
            roundTimer.Elapsed += OnRoundTimeElapsed;
            roundTimer.AutoReset = false;
        }

        // Bắt đầu vòng chơi mới
        public void StartNewRound()
        {
            IsGameStarted = true;
            // Dừng bộ đếm thời gian hiện tại (nếu có)
            roundTimer.Stop();

            currentDrawerIndex = (currentDrawerIndex + 1) % players.Count;
            currentDrawer = players[currentDrawerIndex];
            currentDrawer.IsDrawing = true;

            currentKeyword = GenerateRandomKeyword();

            roundTimer.Start();

        }

        // Sự kiện khi hết thời gian của vòng chơi
        public void OnRoundTimeElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Time is up! Moving to the next round.");
            StartNewRound();
        }


        // Sinh từ khóa ngẫu nhiên từ file Keywords.txt
        public string GenerateRandomKeyword()
        {
            List<string> keywords = new List<string>();

            // Lấy thư mục gốc của ứng dụng
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Lấy đường dẫn tuyệt đối của file Keyword.txt
            string filePath = Path.Combine(projectDirectory, @"..\..\Models\Keyword.txt");

            // Chuyển đường dẫn từ "..\.." thành đường dẫn tuyệt đối
            filePath = Path.GetFullPath(filePath);

            // Loại bỏ phần "Server" trong đường dẫn
            filePath = filePath.Replace(@"\Server", "");

            keywords.AddRange(File.ReadAllLines(filePath));

            if (keywords.Count == 0)
            {
                Console.WriteLine("File Keyword.txt không có từ khóa nào.");
                return "default2";
            }

            return keywords[random.Next(keywords.Count)];
        }

        // Kiểm tra đáp án của người chơi
        public bool CheckAnswer(string guess, User player)
        {
            if (guess.Equals(currentKeyword, StringComparison.OrdinalIgnoreCase))
            {

                player.Score += 10;
                if (currentDrawer != null)
                {
                    currentDrawer.Score += 5;
                }

                roundTimer.Stop();

                StartNewRound();
                return true;
            }
            return false;
        }


        // Lấy danh sách điểm của các người chơi dưới dạng chuỗi
        public string GetScores()
        {
            List<string> scores = new List<string>();
            foreach (var player in players)
            {
                scores.Add($"{player.Name}: {player.Score}");
            }
            return string.Join(", ", scores);
        }
    }
}
