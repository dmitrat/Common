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

## License

Licensed under the Apache License, Version 2.0. See `LICENSE`.

## Attribution (optional)

If you use OutWit.Common.Aspects in a product, a mention is appreciated (but not required), for example:
"Powered by OutWit.Common.Aspects (https://ratner.io/)".

## Trademark / Project name

"OutWit" and the OutWit logo are used to identify the official project by Dmitry Ratner.

You may:
- refer to the project name in a factual way (e.g., "built with OutWit.Common.Aspects");
- use the name to indicate compatibility (e.g., "OutWit.Common.Aspects-compatible").

You may not:
- use "OutWit.Common.Aspects" as the name of a fork or a derived product in a way that implies it is the official project;
- use the OutWit.Common.Aspects logo to promote forks or derived products without permission.
