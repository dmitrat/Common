# OutWit.Common.Aspects

A lightweight runtime AOP library for automating `INotifyPropertyChanged` notifications in.NET.

## Overview

Implementing `INotifyPropertyChanged` is essential for data binding in modern.NET UI frameworks like WPF, MAUI, and Avalonia, but it often leads to verbose, repetitive, and error-prone boilerplate code in your ViewModels.

`OutWit.Common.Aspects` solves this problem by providing a simple, runtime-based "aspect" that automatically injects property change notifications into your classes. 

#### Install

```ps1
Install-Package OutWit.Common.Aspects
```

or

```bash
> dotnet add package OutWit.Common.Aspects
```

#### Before:
```C#
public class MyViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged = delegate { };

    private string m_text;

    public MyViewModel()
    {
        this.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Text")
        {
            Trace.Write("I am here!");
        }
    }

    public string Text
    {
        get => m_text;
        set
        {
            m_text = value;
            PropertyChanged(this, new PropertyChangedEventArgs("Text"));
        }
    }
}
```

#### After:
```C#
    public class MyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public MyViewModel()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((MyViewModel vm) => vm.Text))
            {
                Trace.Write("I am here!");
            }
        }

        [Notify]
        public string Text { get; set; }
    }
```

For more details, check out [the article](https://ratner.io/2024/11/20/streamlining-net-development-with-practical-aspects/).
