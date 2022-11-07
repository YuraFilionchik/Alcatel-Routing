/*
 * Создано в SharpDevelop.
 * Пользователь: user
 * Дата: 04.01.2018
 * Время: 11:58
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;


namespace Routing
{
	public struct stms
		{
			public string name;
			public int stmN;
			public List<int> bordN;

		}
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		string listNames="vols";
		string ListNumbers="names";
		string ListSTM="stmN";
		List<Line> LINES;
		Dictionary<string,string> NUMBERS;
		List<stms> STMs;
		List<string> trassi;
		List<string> AllNodes;
		string SupportedPaths="";
		
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			openFileDialog1.InitialDirectory=Directory.GetCurrentDirectory();
			 LINES=ReadLines(listNames);
			 NUMBERS=ReadNumbers(ListNumbers);
			 STMs=ReadSTMNumber(ListSTM);
			 // reading files
			 var lines=File.ReadAllLines(listNames);
			 var linesNodes=File.ReadAllLines(ListNumbers);
			 trassi=new List<string>();
			 AllNodes=new List<string>();
			 foreach (var line in lines) {
			 	trassi.Add(line.Split(';')[2]);
			 }
			 foreach (var node in linesNodes) {
			 	AllNodes.Add(node.Split(';')[0]);
			 }
			 textBox1.AutoCompleteCustomSource.AddRange(trassi.ToArray());
			 textBox2.AutoCompleteCustomSource.AddRange(AllNodes.ToArray());
		}
		void Button1Click(object sender, EventArgs e)
		{
			
			var dr=openFileDialog1.ShowDialog();
			if(DialogResult.OK!=dr) return;
			string filepath=openFileDialog1.FileName;
			var Routes=ImportRoutes(filepath, true);
			string result="";
			if(Routes==null) {MessageBox.Show("Не удалось импортировать роутинг с файла"); return;}
			
			foreach (var r in Routes) {
			result+=RouteToString(r,"MAIN")+Environment.NewLine;
			}
			
			File.WriteAllText("Main_отчет-"+openFileDialog1.SafeFileName,result);
			
			System.Diagnostics.Process.Start("Main_отчет-"+openFileDialog1.SafeFileName);
		}
		public string ConvertFromAlcatel(string input)
		{	//stm/No
			//01/3/4.3 -input
			var A=input.Split('/'); //01   3   4.3
			string result=A[0];
			int d1=int.Parse(A[1].Trim()); //3
			int d2=int.Parse(A[2].Split('.')[0].Trim());//4
			int d3=int.Parse(A[2].Split('.')[1].Trim());//3
			int N=21*(d1-1)+3*(d2-1)+d3;
			return result+"-"+N;
		}
		
		public List<Route> ImportRoutes(string file, bool Main)
		{ 
			string l="";
			try{
				var inputlines=File.ReadAllLines(file);
				List<Route> result=new List<Route>();
				Route route=new Route();
				List<List<string>> routesLines=new List<List<string>>();
				bool open=false; //начало чтения роутинга Main или Spare
				//bool skipnext=false; //для пропуска строк после spare
				bool isTrail=false;
				List<string> roteLines=new List<string>();
				foreach (var L in inputlines)  //перебор строк входного файла
				{//отделяем список строк каждого роутинга отдельно
					if(L.Contains("=")) continue;
					if(L.Contains("PATH")|| (L.Contains("TRAIL")&&L.Contains("VC12_")))
					{
						if(roteLines.Count!=0) routesLines.Add(roteLines);
						roteLines=new List<string>();						
					}
					roteLines.Add(L);
					
				}
				routesLines.Add(roteLines);
				//теперь все роутинги в отдельных списках
				string node1="";
				string node2="";
				
				foreach(var lst in routesLines)
				{
					//File.WriteAllLines(lst[0].Split('\t')[1].Split('/')[0],lst);
					List<Trail> Trails=GetTrailStrings(lst.ToArray());
					route=new Route();
					//skipnext=false;
					open=false;
					int stmC=0;
					for (int i = 0; i < lst.Count; i++)
					{
						
						l = lst[i];
						//if(String.IsNullOrWhiteSpace(l))continue;
						if (l.Contains("="))
							continue;
						if (l.Contains("MAIN")) {
							if(Main)
							{
							open = true;
							//skipnext=false;
							}else
							{
							open = false;
							node1="";
							node2="";
							}
							continue;
						}
						
						if (l.Contains("TRAIL") && !l.Contains("VC12_")) {
							open = false;
							//skipnext=true;
							continue;
						}
						if (l.Contains("SPARE")) {
							if(Main)
							{
								open = false;
								node1="";
								node2="";
							}
							else //SPARE
							{
								open = true;
								//skipnext=false;;
							}
							continue;
						}
						if(l.Contains("PATH")|| (l.Contains("TRAIL")&&l.Contains("VC12_"))) 
						{
							string lbl="";
							var t=l.Split('\t');
							lbl=t[1];
							route=new Route(lbl);
							open=true;
							continue;
						}
						
						if (!open)
							continue;
						if(node1=="" && String.IsNullOrWhiteSpace(l))continue;
						if(String.IsNullOrWhiteSpace(l)) continue;
						//разбиваем строку на части и анализируем
						var ar2 = l.Split(new [] {"   "},StringSplitOptions.RemoveEmptyEntries);
						var ar=ar2.Where(x=>x!="").ToArray();
						if(ar[0].Contains("c1") || ar[0].Contains("c01"))
	{
		string bp=ar[0].Split('/')[1].TrimEnd();
		string bord=bp.Substring(6,2);
		string port="";
		if(bp.Contains("c1"))//Grodno 3
				port=bp.Substring(10,2);// Grodno_3/r01s1b01p001c1  
		else	port=bp.Substring(9,2);//GR1_04932/r01s1b02p05c01 
		string name=ar[0].Split('/')[0];
		if(stmC==0) {
			if(route.Lines.Count!=0) 
			{//если первая строчка с портом в конце роутинга
				//определение номера stm и порта				
			var stm2=STMs.FirstOrDefault(x=>x.name==name && x.bordN.Contains(int.Parse(bord)));
				if(stm2.stmN!=0) 
				{
			string s2="STM "+stm2.stmN+"-"+(int.Parse(port)+21*stm2.bordN.IndexOf(int.Parse(bord)));
			route.stmEnd=s2;
			stmC=2;
				}
				
			}		else //первая строчка с портом в начале роутинга
			{
				var stm1=STMs.FirstOrDefault(x=>x.name==name && x.bordN.Contains(int.Parse(bord)));
				if(stm1.stmN!=0)
				{
					string s1="STM "+stm1.stmN+"-"+(int.Parse(port)+21*stm1.bordN.IndexOf(int.Parse(bord)));
			route.stmStart=s1;
				}
			
			stmC++;
			}
			
		}//вторая строчка с портом
		else if(stmC==1){
			var st=STMs.FindAll(x=>x.name==name && x.bordN.Contains(int.Parse(bord)));
			
			if(st.Count!=0)
			{
				var stm2=st[0];
				string s2="STM "+stm2.stmN+"-"+(int.Parse(port)+21*stm2.bordN.IndexOf(int.Parse(bord)));
			route.stmEnd=s2;
			stmC++;
			}
			else route.stmEnd="";
			
		}
		
	}
						bool containsDigits= ar.Any(isSTMNo);//??
						if(!containsDigits) continue;
										
						string node=ar[0].Split('/')[0]; //выделение названия узла
						
	
						if(node1=="") 
						{
							node1=node.Split('/')[0];
							continue;
						}else if(node2=="")
						{
							if(node1==node.Split('/')[0]) continue;
							if(!String.IsNullOrWhiteSpace(l)) 
							{//2nd end
								if(ar.Length==3 && (ar[2].Contains("#") ||ar[2].ToLower().Contains("stm")))
								{//is trail
									var trail_name=ar[2].Trim();//название трейла
									var trail=Trails.First(x=>x.name==trail_name);//Трейл_структура
									//проверка направления трейла
									if(node1!=trail.lines[0].node1)//направление трейла не совпадает с трассой
									{
										
										for (int j = trail.lines.Count-1; j>-1 ; j--) 
										{
											var ln=trail.lines[j];
											route.AddLine(ln.node2,ln.node1, ln.Name1+ConvertFromAlcatel(ar[1].Trim()));
										}
									}else
									foreach (var ln in trail.lines)
									{										
										route.AddLine(ln.node1,ln.node2, ln.Name1+ConvertFromAlcatel(ar[1].Trim()));
									}
									node1="";
									node2="";
									continue;
								}
							node2=node.Split('/')[0];
							string trassa="";
							//FIND  NAMES
							var res=LINES.Where(x=>x.node1==node1&&x.node2==node2 ||
							                    x.node1==node2&&x.node2==node1);
							if(res.Count()!=0) trassa=res.First()+"-";
							else trassa="ТРАССА-";
							route.AddLine(node1,node2,trassa+ConvertFromAlcatel(ar[1].Trim()));
							node1="";
							node2="";
							
							}
						}
						
					}//end for 1 route
				result.Add(route);
				}
				return result;
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message+"\n"+l, "import file");		return null;	
			}
		}
	/// <summary>
	/// True if "01/1/2.3" format, нумерация STM-n
	/// </summary>
	/// <param name="N"></param>
	/// <returns></returns>
		public bool isSTMNo(string N)
		{
			if(N==null)return false;
			var ar=N.Trim().Split('/');
			if(N.Trim().Length==8&&ar.Length==3)
				return true;
			return false;
		}//нумерация STM или нет
		public List<stms> ReadSTMNumber(string file)
		{
			try {
				if(!File.Exists(file)) throw new Exception("File not exist");
				var lines=File.ReadAllLines(file);
				List<stms> STMs=new List<stms>();
				foreach (var line in lines)
				{
					stms stm=new stms();
					stm.bordN=new List<int>();
					var ar=line.Split(';');
					stm.name=ar[0];
					stm.stmN=int.Parse(ar[1]);
					var b=ar[2].Split(',');
					if(b.Length!=0)
					foreach (var str in b) {
						stm.bordN.Add(int.Parse(str));
						
					}
					STMs.Add(stm);
					
				}
				return STMs;
			} catch (Exception ex) {
				
				MessageBox.Show(ex.Message,"Чтение файла номеров stm");
				return STMs;
			}
		}
		public List<Line> ReadLines(string file)
		{List<Line> LINES=new List<Line>();
			try {
				var lines=File.ReadAllLines(file);
				
				foreach(string line in lines)
				{ 
					var items=line.Split(';');
					
					LINES.Add(new Line(){
					          	node1=items[0].Trim(),
					          	node2=items[1].Trim(),
					          	Name1=items[2].Trim()
					          });
				}
				return LINES;
			} catch (Exception ex) {
				
				MessageBox.Show(ex.Message,"Ошибка чтения файла с трассами");
				return LINES;
			}
		}
		
		/// <summary>
		/// извлекает часть строк, относящихся к трейлу в данном роутинге
		/// или несколько трейлов 
		/// </summary>
		/// <param name="routelines"></param>
		/// <returns></returns>
		public List<Trail> GetTrailStrings(string[] routelines)
		{
			List<Trail> trails=new List<Trail>();
		try {
				bool istrail=false;
				bool isroutes=false;
				int nEmptyLines=0;
				
				Trail TR=new Trail();
				TR.lines=new List<Line>();
				string node1=""; string node2="";
			foreach (var line in routelines) 
			{
				if(line.Contains("TRAIL") && !line.Contains("VC12_"))
				{
					TR= new Trail();
					TR.lines=new List<Line>();
					istrail=true;
					TR.name=line.Split('\t')[1];//название трейла
					continue;
				}
				if(line.Contains("*")||line.Contains("=")) continue;

				if(!istrail) continue;
				
				if(String.IsNullOrWhiteSpace(line)) 
				{
					nEmptyLines++;
					if(nEmptyLines==2) {isroutes=true; nEmptyLines=0; continue;}
					if(nEmptyLines!=0 && isroutes) 
					{
						isroutes=false;//end of route
						trails.Add(TR);		
						nEmptyLines=0;	
						continue;
					}
					continue;
				}
				if(!isroutes) continue;
				var ar1=line.Split(new []{"   "}, StringSplitOptions.RemoveEmptyEntries);
				var ar2=ar1.Where(x=>x!="").ToArray();
				if(ar2.Length!=2) continue;
				string node=ar2[0].Split('/')[0];
				if(node1=="")
				{
					node1=node;
				}
				else 
				{
					node2=node;
					string trassa="";
					var res=LINES.Where(x=>x.node1==node1&&x.node2==node2 ||
							                    x.node1==node2&&x.node2==node1);
					if(res.Count()!=0) trassa=res.First()+"-";
						else trassa="ТРАССА-";
						
					TR.lines.Add(new Line (){node1=node1,node2=node2,Name1=trassa});
					if(line==routelines.Last()) trails.Add(TR);	
					node1="";
					node2="";
				}
				
			}
			return trails;
				
		} catch (Exception ex) {
			
				MessageBox.Show(ex.Message,"GetTrailPath");
				return trails;
		}
		}
		
		/// <summary>
		/// Чтение файла-списка supported paths
		/// </summary>
		/// <param name="file"></param>
		/// <returns>(user label, comment 1)</returns>
		public Dictionary<string,string> ReadLabelsFromFile(string file)
		{
			Dictionary<string,string> labels=new Dictionary<string, string>();
			try {
				if(!File.Exists(file)) throw new Exception("Файл не найден -"+file);
				var lines=File.ReadAllLines(file);
				foreach (var line in lines) {
					if(line.Split(',').Length<2) continue;
					labels.Add(line.Split(',')[0],line.Split(',')[1]);
				}
				return labels;
			} catch (Exception ex) {
				MessageBox.Show(ex.Message,"Чтение файла с нумерацией потоков");
				return labels;
			}
		}
		public Dictionary<string,string> ReadNumbers(string file)
		{
			Dictionary<string,string> LISTS=new Dictionary<string,string>();
			try
			{
				var lines=File.ReadAllLines(file);
				foreach (var l in lines) 
				{
					
					var items=l.Split(';');
					if(items.Length!=3) continue;
					LISTS.Add(items[0].Trim(),items[1].Trim());
				}
				return LISTS;
			} catch (Exception ex) 
			{
			MessageBox.Show(ex.Message,"ReadNumbers");
			return LISTS;
			}
		}
		void Button2Click(object sender, EventArgs e)
		{
	var dr=openFileDialog1.ShowDialog();
			if(DialogResult.OK!=dr) return;
			string filepath=openFileDialog1.FileName;
			var Routes=ImportRoutes(filepath,false);
			string result="";
			if(Routes==null) {MessageBox.Show("Не удалось импортировать роутинг с файла"); return;}
			Dictionary<string,string> Labels=new Dictionary<string, string>();
			if(File.Exists(SupportedPaths)) Labels=ReadLabelsFromFile(SupportedPaths);
			foreach (var r in Routes) {
				if(Labels.ContainsKey(r.Label))
				result+=r.Label+"\t"+Labels[r.Label]+Environment.NewLine;
				else	result+=r.Label+Environment.NewLine;
				
				result+=RouteToString(r,"SPARE");
			}
			
			File.WriteAllText("Spare_отчет-"+openFileDialog1.SafeFileName,result);
			
			System.Diagnostics.Process.Start("Spare_отчет-"+openFileDialog1.SafeFileName);
		}
		void TextBox1TextChanged(object sender, EventArgs e)
		{
	
		}
		
		//Start button
		void Button3Click(object sender, EventArgs e)
		{	int countTrue=0;
			Dictionary<string,string> Labels=new Dictionary<string, string>();
			if(string.IsNullOrWhiteSpace(textBox1.Text)) return;
	var dr=openFileDialog1.ShowDialog();
			if(DialogResult.OK!=dr) return;
			string filepath=openFileDialog1.FileName;
			var Routes=ImportRoutes(filepath, true);
			string result="";
			if(Routes==null) {MessageBox.Show("Не удалось импортировать роутинг с файла"); return;}
			if(File.Exists(SupportedPaths)) Labels=ReadLabelsFromFile(SupportedPaths);
			foreach (var r in Routes) {
				
				if (r.Lines.Any(x => x.Name1.Contains(textBox1.Text)&&
				                (x.node1.Contains(textBox2.Text.Trim()) || x.node2.Contains(textBox2.Text.Trim()) || String.IsNullOrWhiteSpace(textBox1.Text))))
					countTrue++;
				else	continue; //filtr by trassa and node
//				if(Labels.ContainsKey(r.Label))
//						result+=r.Label+"\t"+Labels[r.Label]+Environment.NewLine;
//				else	result+=r.Label+Environment.NewLine;
				result+=RouteToString(r,"MAIN")+Environment.NewLine;
			}
			
			File.WriteAllText("Main_отчет- "+textBox1.Text+" "+openFileDialog1.SafeFileName,result);
			
			System.Diagnostics.Process.Start("Main_отчет- "+textBox1.Text+" "+openFileDialog1.SafeFileName);
		}
		void Button4Click(object sender, EventArgs e)
		{
			openFileDialog2.InitialDirectory=Directory.GetCurrentDirectory();
			
			
			DialogResult dr=	openFileDialog2.ShowDialog();		
			if(dr==DialogResult.OK)	
			{
				SupportedPaths=openFileDialog2.FileName;
				label3.Text=SupportedPaths;
			}
			
		
		}
		//main+spare
		void Button5Click(object sender, EventArgs e)
		{
	var dr=openFileDialog1.ShowDialog();
			if(DialogResult.OK!=dr) return;
			string filepath=openFileDialog1.FileName;
			var MainRoutes=ImportRoutes(filepath, true);
			var SpareRoutes=ImportRoutes(filepath,false);
			string result="";
			if(MainRoutes==null) {MessageBox.Show("Не удалось импортировать Main роутинг с файла"); return;}
			if(SpareRoutes==null) {MessageBox.Show("Не удалось импортировать Spare роутинг с файла"); return;}
			
			foreach (var r in MainRoutes) {
				Route SR=SpareRoutes.Find(x=>x.Label==r.Label);
				result+=RouteToString(r,"MAIN");
				result+=RouteToString(SR,"SPARE");
			}
			
			File.WriteAllText("MainSpare-"+openFileDialog1.SafeFileName,result);
			System.Diagnostics.Process.Start("MainSpare-"+openFileDialog1.SafeFileName);
		}
		/// <summary>
		/// Переводит роутинг в строковое представление, итоговое
		/// </summary>
		/// <param name="r">Роутинг для вывода</param>
		/// <param name="type">MAIN or SPARE</param>
		/// <returns></returns>
		public string RouteToString(Route r, string type)
		{
			string result="";
			Dictionary<string,string> Labels=new Dictionary<string, string>();
			if(File.Exists(SupportedPaths)) Labels=ReadLabelsFromFile(SupportedPaths);
			
				
			//	bool first=true;
				if(type=="MAIN") {
					
					if(Labels.ContainsKey(r.Label))
						result+=r.Label+"\t"+Labels[r.Label]+Environment.NewLine;
				else	result+=r.Label+Environment.NewLine;
				
				Route route_mod=Route_for_listing(r);
				if(route_mod.Lines.Count==1) //Одна линия
					result+="MAIN:\t" +route_mod.stmStart + " ("+route_mod.Lines[0].node1+")"+route_mod.Lines[0].Name1+"("+route_mod.Lines[0].node2+")\t"+route_mod.stmEnd;
				else //много линий
				for (int i = 0; i < route_mod.Lines.Count; i++)
				{
					var line=route_mod.Lines[i];
					Line l_prev;
					if(i==0) result+="MAIN:\t" +route_mod.stmStart + " ("+line.node1+")"+line.Name1+"("+line.node2+")";
					else if(i < route_mod.Lines.Count-1 ) {//not last
						l_prev=route_mod.Lines[i-1];
						if(line.node2==l_prev.node2) continue;
						result += "x"+line.Name1 + "(" + line.node2 + ")";
					} else //last
					{
					l_prev=route_mod.Lines[i-1];
						if(line.node2==l_prev.node2) result+="\t"+route_mod.stmEnd;
						else result+="x"+line.Name1 + "(" + line.node2 + ")"+"\t"+route_mod.stmEnd;
					}
				}
				
				}	
				else //SPARE
				{
				Route route_mod=Route_for_listing(r);
				
				for (int i = 0; i < route_mod.Lines.Count; i++)
				{
					var line=route_mod.Lines[i];
					Line l_prev;
					if(i==0) result+="SPARE:\t" + " ("+line.node1+")"+line.Name1+"("+line.node2+")";
					else if(i < route_mod.Lines.Count-1 ) {//not last
						l_prev=route_mod.Lines[i-1];
						if(line.node2==l_prev.node2) continue;
						result +="x"+line.Name1 + "(" + line.node2 + ")";
					} else {//last
						l_prev=route_mod.Lines[i-1];
						if(line.node2==l_prev.node2) continue;
						else result+="x"+line.Name1 + "(" + line.node2 + ")";
					}
				}
				result+=Environment.NewLine;
				}
				result+=Environment.NewLine;
			result=result.Replace("--5---01-","-5-");
			result=result.Replace("--5---02-","-6-");
			result=result.Replace("--5---03-","-7-");
			result=result.Replace("--5---04-","-8-");
			return result;
		}
		/// <summary>
		/// Промежуточный роутинг, с переименованными узлами Node1 и Node 2
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		Route Route_for_listing(Route r)
		{
			Route route_mod=new Route(r.Label);
				route_mod.stmEnd=r.stmEnd;
				route_mod.stmStart=r.stmStart;			
			for (int i = 0; i < r.Lines.Count; i++) 
				{
					
					string node1N="";
					string node2N="";
					var l = r.Lines[i];
					if(NUMBERS.ContainsKey(l.node1))
						node1N=NUMBERS[l.node1]; else node1N=l.node1;
					if(NUMBERS.ContainsKey(l.node2))
					node2N=NUMBERS[l.node2]; else node2N=l.node2;
					if(node1N==node2N)		 //remove dublicates						
						continue;
					
					route_mod.Lines.Add(new Line(){Name1=l.Name1, node1=node1N, node2=node2N});

				}
			if(route_mod.Lines.Count>2 && (route_mod.Lines.Last().node2 == "04932" || route_mod.Lines.Last().node1 == "04932")
			   || (route_mod.Lines.Count>0 && route_mod.Lines.Last().node2 == "04932")) return Route_revers(route_mod);
			else return route_mod;
		}
		/// <summary>
		/// Меняет направление роутинга
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		Route Route_revers(Route r)
		{
			Route route_mod=new Route(r.Label);
			route_mod.stmEnd=r.stmStart;
			route_mod.stmStart=r.stmEnd;
			for (int i = r.Lines.Count-1; i >= 0; i--) 
				{
					string node1N="";
					string node2N="";
					var l = r.Lines[i];
					node1N=l.node1;
					node2N=l.node2;
					route_mod.Lines.Add(new Line(){Name1=l.Name1, node2=node1N, node1=node2N});

				}
			return route_mod;
		}
		void MainFormLoad(object sender, EventArgs e)
		{
	
		}
		
	
	}
}
