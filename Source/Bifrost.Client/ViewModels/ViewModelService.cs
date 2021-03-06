﻿#region License
//
// Copyright (c) 2008-2015, Dolittle (http://www.dolittle.com)
//
// Licensed under the MIT License (http://opensource.org/licenses/MIT)
//
// You may not use this file except in compliance with the License.
// You may obtain a copy of the license at 
//
//   http://github.com/dolittle/Bifrost/blob/master/MIT-LICENSE.txt
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

#if(NETFX_CORE)
using Windows.UI.Xaml;

using Windows.Storage;
#else
using System.Windows;
#endif

namespace Bifrost.ViewModels
{
    public delegate Type FindTypeByName(string name);
    public delegate object CreateInstance(Type type);

    public class ViewModelService
    {
#if(NETFX_CORE)
        static IEnumerable<Assembly> CollectAssemblies()
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var assemblies = new List<Assembly>();

            IEnumerable<StorageFile> files = null;
            folder.GetFilesAsync().AsTask().ContinueWith(f => files = f.Result).Wait();

            foreach (var file in files)
            {
                if (file.FileType == ".dll" || file.FileType == ".exe")
                {
                    var name = new AssemblyName() { Name = System.IO.Path.GetFileNameWithoutExtension(file.Name) };
                    try
                    {
                        Assembly asm = Assembly.Load(name);
                        assemblies.Add(asm);
                    }
                    catch { }
                }
            }
            return assemblies;
        }
#else

		static IEnumerable<Assembly> CollectAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var query = from a in assemblies
						where !a.FullName.Contains("System.")
						select a;

			return assemblies;
		}
#endif


        public static FindTypeByName TypeFinder = (string name) =>
        {
            var assemblies = CollectAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(name);
                if (type != null) return type;
            }
            return null;
        };
        public static CreateInstance InstanceCreator = (Type type) => Activator.CreateInstance(type);

        public static DependencyProperty ViewModelDependencyProperty =
            DependencyProperty.RegisterAttached("ViewModel", typeof(string), typeof(ViewModelService), null);

        public static void SetViewModel(FrameworkElement target, string viewModel)
        {
            var type = TypeFinder(viewModel);
            

            /*
            var typeDiscoverer = App.Container.Get<ITypeDiscoverer>();
            var types = typeDiscoverer.FindAnyByName(viewModel);
            if (types.Length > 1) throw new ArgumentException("Ambiguous viewModel name for : " + viewModel);
             * */

            var viewModelInstance = InstanceCreator(type);
                //App.Container.Get(types[0]);
            target.DataContext = viewModelInstance;
        }

        public static string GetViewModel(FrameworkElement target)
        {
            if (target.DataContext == null) return string.Empty;
            return target.DataContext.GetType().Name;
        }
    }
}
