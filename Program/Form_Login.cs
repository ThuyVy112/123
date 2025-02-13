﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1;
using Models;

namespace Program
{
    public partial class Form_Login : Form
    {
        private Client client;
        public Form_Login()
        {
            InitializeComponent();
            this.client = Form_Input_ServerIP.client;
            client.LoginSuccessful += OnLoginSuccessful;
        }

        private void loginButton_Click_1(object sender, EventArgs e)
        {
            string username = usernameTextbox.Text;
            string password = passTextbox.Text;

            if (username == "" || password == "")
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin");
                return;
            }

            try
            {
                // Gửi thông tin đăng nhập lên server
                LoginPacket packet = new LoginPacket($"{username};{password}");
                client.SendPacket(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi gửi thông tin đăng nhập: {ex.Message}");
            }
        }
        private void showPwCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (showPwCheckBox.Checked)
            {
                passTextbox.UseSystemPasswordChar = false; // Hiển thị mật khẩu
            }
            else
            {
                passTextbox.UseSystemPasswordChar = true; // Ẩn mật khẩu
            }
        }
        
        private void OnLoginSuccessful()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnLoginSuccessful));
                return;
            }

            string username = usernameTextbox.Text;
            Form_Home homeForm = new Form_Home(username);
            homeForm.StartPosition = FormStartPosition.Manual; // Đặt hiển thị theo tọa độ
            homeForm.Location = this.Location; // Đặt vị trí của Form_Home giống với Form_Background
            homeForm.Show();
            this.Hide();
            homeForm.FormClosed += (s, args) => this.Close();
        }



        private void lawBtn_Click(object sender, EventArgs e)
        {
            Form_Law lawForm = new Form_Law();
            this.Hide(); 
            lawForm.StartPosition = FormStartPosition.Manual;
            lawForm.Location = new Point(this.Location.X, this.Location.Y);
            lawForm.ShowDialog();
            this.Show();
            lawForm.FormClosed += (s, args) => this.Close();
        }


        private void regBtn_Click(object sender, EventArgs e)
        {
            Form_Register regForm = new Form_Register();
            this.Hide();
            regForm.StartPosition = FormStartPosition.Manual;
            regForm.Location = new Point(this.Location.X, this.Location.Y);
            regForm.ShowDialog();
            this.Show();
            regForm.FormClosed += (s, args) => this.Close();
        }

        private void Form_Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectPacket disconnectPacket = new DisconnectPacket("");
            client.SendPacket(disconnectPacket);
            //client.Stop();
        }

        private void forgetLabel_Click(object sender, EventArgs e)
        {
            Form_Forget_Password forgetForm = new Form_Forget_Password();
            this.Hide();
            forgetForm.StartPosition = FormStartPosition.Manual;
            forgetForm.Location = new Point(this.Location.X, this.Location.Y);
            forgetForm.ShowDialog();
            this.Show();
            forgetForm.FormClosed += (s, args) => this.Close();
        }
    }
}