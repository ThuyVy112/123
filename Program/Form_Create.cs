﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Models;
using ReaLTaiizor.Forms;

namespace Program
{
    public partial class Form_Create : Form
    {
        private string username;
        private Client client;
        public Form_Create(string username)
        {
            InitializeComponent();
            this.client = Form_Input_ServerIP.client;
            this.username = username;
        }

        private void createBtn_Click(object sender, EventArgs e)
        {

            int maxPlayers = (int)numeric.ValueNumber;
            CreateRoomPacket createRoomPacket = new CreateRoomPacket($"{username};{maxPlayers}");
            client.SendPacket(createRoomPacket);

            //đóng form create sau khi tạo phòng
            this.Close();

        }

        private void Form_Create_Load(object sender, EventArgs e)
        {
            numeric.MinNum = 1;
            numeric.MaxNum = 5;
            numeric.ValueNumber = 2;
        }

    }
}
