using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Collections;
using Microsoft.Office.Interop.Excel;
using System.IO;

namespace quickmailer
{
    public partial class Form1 : Form
    {
        private Workbook ewb;
        private Worksheet ews;
        private Microsoft.Office.Interop.Excel.Application ea;
        private static MailMessage []msgs;
        private cmailer[] mailers;
        private NetworkCredential nc;
        private long mail_count = 0;
        const long MAX_COUNT = 20;


        public static void deletemessage(int index)
        {
            msgs[index].Dispose();
        }

        public Form1()
        {
            InitializeComponent();
            txtUsername.Text = "";
            txtPassword.Text = "";
            txtSubject.Text = "";
            cbName.Items.Add((string)"");
            cbName.Items.Add((string)"");
            cbName.SelectedIndex = 0;
            nc = new NetworkCredential(txtUsername.Text, txtPassword.Text);
            mailers = new cmailer[MAX_COUNT];
            msgs = new MailMessage[MAX_COUNT];
        }

        private void mnuOpenSource_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Excel 2007|*.xlsx|Excel 2003|*.xls";

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ea = new Microsoft.Office.Interop.Excel.Application();
                ewb = ea.Workbooks.Open(ofd.FileName);
                ews = ewb.Worksheets[1];
                int rs;

                mainlist.Items.Clear();
                ListViewItem li;
                rs = 2;
                while (ews.Cells[rs, 1].Value!=null )
                {
                    String s = (string)ews.Cells[rs, 2].Value;
                    li = mainlist.Items.Add(s);
                    li.SubItems.Add((string)ews.Cells[rs, 3].Value);
                    li.SubItems.Add((string)ews.Cells[rs, 4].Value);
                    li.SubItems.Add((string)ews.Cells[rs, 5].Value);
                    li.SubItems.Add((string)ews.Cells[rs, 6].Value);
                    li.SubItems.Add((string)ews.Cells[rs, 7].Value);
                    li.SubItems.Add("");
                    rs++;
                }
                ewb.Close();
                ea.Quit();
            }
        }

        private void mnuOpenMessageBody_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Message Body|*.txt";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StreamReader sr;

                sr = new StreamReader(ofd.FileName);
                sr.Close();
            }
        }

        private void mnuSend_Click(object sender, EventArgs e)
        {
            string s_message;
            string []s_emails;


            s_message = (File.OpenText(txtContent.Text)).ReadToEnd();

            mail_count = 0;
            foreach (ListViewItem lv in mainlist.Items)
            {
                if (lv.Checked) 
                {
                    /*if (!File.Exists(lv.SubItems[2].Text))
                    {
                        lv.SubItems[3].Text = "No attachment!";
                        continue;
                    }*/

                    if (lv.SubItems[5].Text.Length == 0)
                    {
                        lv.SubItems[5].Text = "No email address!";
                        continue;
                    }


                    s_emails = lv.SubItems[5].Text.Split(',', ';');


                    foreach (string se in s_emails)
                    {
                        lv.SubItems[6].Text = "Sending...";
                        mailers[mail_count] = new cmailer(nc);
                        msgs[mail_count] = new MailMessage(txtEmail.Text, se);
                        msgs[mail_count].From = new MailAddress(txtEmail.Text, cbName.Text);

                        msgs[mail_count].Body = s_message;
                        msgs[mail_count].Subject = txtSubject.Text + " FOR " + lv.Text.ToUpper();
                        msgs[mail_count].IsBodyHtml = true;
                        msgs[mail_count].BodyEncoding = ASCIIEncoding.Default;

                        //Attachment a = new Attachment(lv.SubItems[2].Text);
                        //msgs[mail_count].Attachments.Add(a);

                        lvx t = new lvx();
                        t.lvi = lv;
                        t.rowindex = (int)mail_count;
                        mailers[mail_count].sendit(msgs[mail_count], t);
                        mail_count++;
                    }
                }
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            nc.UserName = txtUsername.Text;
            nc.Password = txtPassword.Text;
            MessageBox.Show("Credentials changed!");
        }

        private void txtContent_Click(object sender, EventArgs e)
        {
            ofd.Filter = "HTML|*.html|HTM|*.htm";

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtContent.Text = ofd.FileName;
            }
        }

        private void cbName_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbName.SelectedIndex)
            {
                case 0:
                    txtEmail.Text = "";
                    break;

                case 1:
                    txtEmail.Text = "";
                    break;
            }
        }
    }

    class cmailer
    {
        private SmtpClient mc;
        private Attachment m;

        public cmailer(NetworkCredential nc)
        {
            mc = new SmtpClient("smtp.googlemail.com");
            mc.Port = 587;
            mc.EnableSsl = true;
            mc.Credentials = nc;
            mc.SendCompleted+=new SendCompletedEventHandler(SendCompleted);
        }


        public void sendit(MailMessage m,lvx token)
        {
            mc.SendAsync(m, token);
        }

        private static void SendCompleted(Object o, AsyncCompletedEventArgs a)
        {
            lvx token;

            token = (lvx)a.UserState;

            if (a.Error != null)
            {
                token.lvi.SubItems[6].Text =a.Error.Message;
            }
            else
            {
                token.lvi.SubItems[6].Text = "OK";
                token.lvi.Checked = false;
            }
            Form1.deletemessage(token.rowindex);
            ((SmtpClient)o).Dispose();                                                             
        }
    }


    class lvx
    {
        public ListViewItem lvi;
        public int rowindex;
    }
}
