/*
 * Создано в SharpDevelop.
 * Пользователь: user
 * Дата: 04.01.2018
 * Время: 12:08
 * 
 * Для изменения этого шаблона используйте меню "Инструменты | Параметры | Кодирование | Стандартные заголовки".
 */
using System;
using System.Collections.Generic;

namespace Routing
{
	/// <summary>
	/// Description of Route.
	/// </summary>
	public class Route
	{
		public string Label;
		public List<Line> Lines;
		public string stmStart;
		public string stmEnd;
		public Route()
		{		
			Label="";
			Lines=new List<Line>();
			stmStart="";
			stmEnd="";
		}
			public Route(string label)
		{		
			Label=label;
			Lines=new List<Line>();
			stmStart="";
			stmEnd="";
		}
			public void AddLine(string node1, string node2, string name)
			{
				Lines.Add(new Line(){node1=node1,node2=node2,Name1=name});
			}

	}
	
	public struct Line
	{
		public string node1;
		public string node2;
		public string Name1;
		public override string ToString()
		{
			return string.Format("{0}", Name1);
		}

	}
	public struct Trail
	{
		public string name;
		public List<Line> lines;

	}
}
