﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Money.Views.Navigation
{
    internal class ApplicationNavigatorForm : PageNavigatorForm
    {
        private readonly Template template;

        public ApplicationNavigatorForm(Template template, Type pageType, object parameter) 
            : base(template.ContentFrame, pageType, parameter)
        { }

        public override void Show()
        {
            base.Show();
            template.ShowLoading();
        }
    }
}
