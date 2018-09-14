using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Text;

namespace Postal.AspNetCore
{
    public interface IViewData
    {
        ViewDataDictionary ViewData { get; set; }
    }
}
