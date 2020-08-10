﻿using BlankCoreAppCopyTask.Services;
using BlankCoreAppCopyTask.Views;
using Prism.Ioc;
using System.Windows;

namespace BlankCoreAppCopyTask
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IHashCalculator, HashService>();
            containerRegistry.Register<ISynchronizationPlaylist, SynchronizationPlaylist>("VerFast");
            containerRegistry.Register<ISynchronizationPlaylist, SynchronizationPlaylistSlow>("VerSlow");
        }
    }
}
