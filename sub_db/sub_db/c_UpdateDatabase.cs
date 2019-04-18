﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace sub_db
{
	public partial class c_UpdateDatabase : Form
	{
		public c_UpdateDatabase()
		{
			InitializeComponent();
		}

		// 是否正在更新数据库
		internal bool	m_is_updating_database	= false;
		internal bool	m_is_stopping			= false;

		#region Winform 事件
		/*==============================================================
		 * 窗口加载时
		 *==============================================================*/
		private void c_UpdateDatabase_Load(object sender, EventArgs e)
		{
			this.Icon	= c_Image_.img2icon(Resource1.Logo);
			this.Text	= c_Languages_.txt(10);	// 更新数据库
		}

		/*==============================================================
		 * 窗口关闭时
		 *==============================================================*/
		private void c_UpdateDatabase_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			this.Hide();
		}

		/*==============================================================
		 * ListView 调整大小
		 *==============================================================*/
		private void ListView_Log_Resize(object sender, EventArgs e)
		{
			columnHeader_Time.Width = 122;
			columnHeader_Log.Width = listView_Log.Width - columnHeader_Time.Width - 21;
		}

		/*==============================================================
		 * 开始
		 *==============================================================*/
		private void PictureBox_Start_Click(object sender, EventArgs e)
		{
			append_log(c_Languages_.txt(54));	// 执行数据库更新...
			m_is_updating_database	= true;
			m_is_stopping			= false;

			lock_controls(false);

			Thread th = new Thread(update_db_thread);
			th.Start();
		}

		/*==============================================================
		 * 停止
		 *==============================================================*/
		private void PictureBox_Stop_Click(object sender, EventArgs e)
		{
			m_is_stopping = true;
		}
		#endregion

		/*==============================================================
		 * 锁定控件
		 *==============================================================*/
		void lock_controls(bool enabled)
		{
			pictureBox_Start.Enabled	= enabled;
			pictureBox_Start.Image		= enabled ? Resource1.Start : Resource1.Start2;
			pictureBox_Stop.Enabled		= !enabled;
			pictureBox_Stop.Image		= enabled ? Resource1.Stop2 : Resource1.Stop;

			label_Log.Visible			= !enabled;
			progressBar_Update.Visible	= !enabled;
		}

		/*==============================================================
		 * 追加日志
		 *==============================================================*/
		void append_log(string txt, Color color = default)
		{
			if(color == default)
				color = Color.Black;

			ListViewItem LVI = new ListViewItem();
			while(LVI.SubItems.Count < listView_Log.Columns.Count)
				LVI.SubItems.Add("");

			DateTime time = DateTime.Now;

			LVI.SubItems[0].Text = string.Format(	"{0:d4}-{1:d2}-{2:d2} {3:d2}:{4:d2}:{5:d2}",
													time.Year, time.Month, time.Day,
													time.Hour, time.Minute, time.Second );
			LVI.SubItems[1].Text = txt;

			LVI.ForeColor = color;

			listView_Log.Items.Add(LVI);

			LVI.EnsureVisible();
		}

		/*==============================================================
		 * 执行更新数据库的线程
		 *==============================================================*/
		void update_db_thread()
		{
			//if(m_is_stopping)
			//	return;

			if(!Directory.Exists(c_Settings_.m_s_subs_path))
			{
				void func()
				{
					append_log(	$"{c_Languages_.txt(55)} {c_Settings_.m_s_subs_path}",	// 找不到字幕路径
								Color.Red );
					lock_controls(true);
				}
				c_Forms_.Invoke(func);
				return;
			}

			Stopwatch sw = new Stopwatch();
			sw.Start();

			c_Data_.m_s_lock.EnterWriteLock();

			c_Data_.m_s_all_subs.Clear();

			void load_func()
			{
				string[] dirs_type = Directory.GetDirectories(c_Settings_.m_s_subs_path, "*.*", SearchOption.TopDirectoryOnly);
				foreach(string dir_type in dirs_type)
				{
					string[] dirs_year = Directory.GetDirectories(dir_type, "*.*", SearchOption.TopDirectoryOnly);
					foreach(string dir_year in dirs_year)
					{
						string[] dirs_video = Directory.GetDirectories(dir_year, "*.*", SearchOption.TopDirectoryOnly);
						foreach(string dir_video in dirs_video)
						{
							void update_label()
							{
								label_Log.Text = dir_video;
							}
							c_Forms_.Invoke(update_label);

							// 日期
							string name_tmp = Path.GetFileName(dir_video);
							int idx = name_tmp.IndexOf(")");
							string[] date_vals = name_tmp.Substring(1, idx - 1).Split('.');

							int year	= int.Parse(date_vals[0]);
							int month	= (date_vals.Length >= 2) ? int.Parse(date_vals[1]) : 1;
							int day		= (date_vals.Length >= 3) ? int.Parse(date_vals[2]) : 1;

							DateTime date = new DateTime(year, month, day);

							// 番名
							string name_chs		= dir_video.Substring(idx + 1);
							string name_cht		= "";
							string name_jp		= "";
							string name_en		= "";
							string name_rome	= "";

							// 读取 info.txt
							string info_filename = Path.Combine(dir_video, "info.txt");

							if(File.Exists(info_filename))
							{
								string[] lines = File.ReadAllLines(info_filename, Encoding.UTF8);
								foreach(string line in lines)
								{
									if(line.Length < 2)
										continue;

									if(line[0] == '/' && line[1] == '/')
										continue;

									idx = line.IndexOf(":");
									if(idx < 0)
										continue;

									string w1 = line.Substring(0, idx).Trim().ToLower();
									string w2 = line.Substring(idx + 1).Trim();

									if(w1 == "chs")
									{
										name_chs = w2.Trim();
										continue;
									}

									if(w1 == "cht")
									{
										name_cht = w2.Trim();
										continue;
									}

									if(w1 == "jp")
									{
										name_jp = w2.Trim();
										continue;
									}

									if(w1 == "en")
									{
										name_en = w2.Trim();
										continue;
									}

									if(w1 == "rome")
									{
										name_rome = w2.Trim();
										continue;
									}
								}	// for
							}

							string[] dirs_matches = Directory.GetDirectories(dir_video, "*.*", SearchOption.TopDirectoryOnly);
							foreach(string dir_matches in dirs_matches)
							{
								string[] dirs_sub_name = Directory.GetDirectories(dir_matches, "*.*", SearchOption.TopDirectoryOnly);
								foreach(string dir_sub_name in dirs_sub_name)
								{
									if(m_is_stopping)
										return;

									string	providers	= "";	// 字幕组/提供者
									string	desc		= "";	// 字幕说明

									// 后缀名
									SortedSet<string> extension_list = new SortedSet<string>();

									string[] files = Directory.GetFiles(dir_sub_name, "*.*", SearchOption.TopDirectoryOnly);
									foreach(string file in files)
									{
										string extension = Path.GetExtension(file).ToLower();
										extension_list.Add(extension);
									}	// for files

									// xxx.txt
									string desc_filename = dir_sub_name + ".txt";
									if(File.Exists(desc_filename))
									{
										string file_txt = File.ReadAllText(desc_filename, Encoding.UTF8);
										idx = file_txt.IndexOf("desc:", StringComparison.CurrentCultureIgnoreCase);
										if(idx > 0)
											desc = file_txt.Substring(idx + 5).Trim();

										string[] lines = File.ReadAllLines(desc_filename, Encoding.UTF8);
										foreach(string line in lines)
										{
											if(line.Length < 2)
												continue;

											if(line[0] == '/' && line[1] == '/')
												continue;

											idx = line.IndexOf(":");
											if(idx < 0)
												continue;

											string w1 = line.Substring(0, idx).Trim().ToLower();
											string w2 = line.Substring(idx + 1).Trim();

											if(w1 == "providers")
											{
												providers = w2.Trim();
												break;
											}
										}	// for
									}

									c_Data_.c_SubInfo sub_info = new c_Data_.c_SubInfo();
									c_Data_.m_s_all_subs.Add(sub_info);

									sub_info.m_name_chs		= name_chs;
									sub_info.m_name_cht		= name_cht;
									sub_info.m_name_jp		= name_jp;
									sub_info.m_name_en		= name_en;
									sub_info.m_name_rome	= name_rome;

									sub_info.m_time			= date;
									sub_info.m_type			= Path.GetFileName(dir_type);
									sub_info.m_match		= Path.GetFileName(dir_matches);
									sub_info.m_sub_name		= Path.GetFileName(dir_sub_name);

									foreach(string extension in extension_list)
									{
										if(sub_info.m_extension.Length > 0)
											sub_info.m_extension += ";";

										sub_info.m_extension += extension;
									}

									sub_info.m_providers	= providers;
									sub_info.m_desc			= desc;
								}	// for dirs_sub_name
							}	// for dirs_matches
						}	// for dirs_video
					}	// for dirs_year
				}	// for dirs_type
			}

			c_Forms_.Invoke(load_func);

			c_Data_.m_s_lock.ExitWriteLock();

			void done_func()
			{
				sw.Stop();

				lock_controls(true);

				if(m_is_stopping)
				{
					append_log(	string.Format(	$"{c_Languages_.txt(56)}{c_Languages_.txt(58)}",	// 用户停止更新数据库（合计 {0:d} 条数据，耗时 {1:s}）
												c_Data_.m_s_all_subs.Count,
												sw.Elapsed.ToString() ),
								Color.Blue );
				}
				else
				{
					append_log(	string.Format(	$"{c_Languages_.txt(57)}{c_Languages_.txt(58)}",	// 更新数据库完成（合计 {0:d} 条数据，耗时 {1:s}）
												c_Data_.m_s_all_subs.Count,
												sw.Elapsed.ToString() ),
								Color.Green );
				}

				c_Data_.update_DataTable();
				c_Mainform.m_s_mainform.dataGridView_Main.DataSource = c_Data_.m_s_dt;

				//c_Mainform.m_s_mainform.dataGridView_Main.Columns["extension"].Width = 50;

				m_is_stopping = false;
			}
			c_Forms_.Invoke(done_func);
		}
	};
}	// namespace sub_db
