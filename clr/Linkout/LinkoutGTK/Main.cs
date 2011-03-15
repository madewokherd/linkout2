/*
 * Copyright 2010, 2011 Vincent Povirk
 * 
 * This file is part of Linkout.
 *
 * Linkout is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.IO;
using System.Reflection;
using Mono.Unix;
using Gtk;
using Nini.Config;

namespace LinkoutGTK
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Catalog.Init("i18n", "./locale"); // probably not good enough for actual translation
			Application.Init ();
			using (MainWindow win = new MainWindow ())
			{
				win.Show ();
				Application.Run ();
			}
		}
		
		private static string UserConfigPath ()
		{
			string result;
			result = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			result = Path.Combine(result, "linkout");
			result = Path.Combine(result, "linkout.ini");
			return result;
		}
		
		private static void AddAliases (AliasText alias)
		{
			alias.AddAlias("Y", true);
			alias.AddAlias("N", false);
			alias.AddAlias("Yes", true);
			alias.AddAlias("No", false);
			alias.AddAlias("T", true);
			alias.AddAlias("F", false);
			alias.AddAlias("True", true);
			alias.AddAlias("False", false);
			alias.AddAlias("Show", true);
			alias.AddAlias("Hide", false);
			alias.AddAlias("On", true);
			alias.AddAlias("Off", false);
		}
		
		private static IConfigSource GetAppConfig ()
		{
			IniConfigSource result, temp;
			string shared_config_path, exe_path, exe_config_path;
			
			result = new IniConfigSource();

			AddAliases(result.Alias);
			
			result.AddConfig("EditMode").Set("ShowSeekbar", "yes");
			
			shared_config_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			shared_config_path = Path.Combine(shared_config_path, "linkout");
			shared_config_path = Path.Combine(shared_config_path, "linkout.ini");
			try
			{
				temp = new IniConfigSource(shared_config_path);
				AddAliases(temp.Alias);
				result.Merge(temp);
			}
			catch
			{
			}
			
			exe_path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			exe_config_path = Path.Combine(exe_path, "linkout.ini");
			try
			{
				temp = new IniConfigSource(exe_config_path);
				AddAliases(temp.Alias);
				result.Merge(temp);
			}
			catch
			{
			}
			
			try
			{
				temp = new IniConfigSource(UserConfigPath());
				AddAliases(temp.Alias);
				result.Merge(temp);
			}
			catch
			{
			}
			
			return result;
		}
		
		public static void SaveConfig ()
		{
			string path = UserConfigPath();
			
			System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));

			((IniConfigSource)Config).Save(path);
		}
		
		public static readonly IConfigSource Config = GetAppConfig();
	}
}

