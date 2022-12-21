using SDKTemplate;
using System;

namespace WindowsFormsApp1
{
    public interface IComsPanel
    {
        MainPage MainPage { get; set; }

        string Value { get; set; }
    }

    class Constants
    {
    }
}
