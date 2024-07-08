﻿using System.Windows;
using System.Windows.Input;

using NEngineEditor.Commands;
using NEngineEditor.ViewModel;

namespace NEngineEditor.Windows;
/// <summary>
/// Interaction logic for NewItemDialog.xaml
/// </summary>
public partial class NewItemDialog : Window
{
    public string CreateItemTypeString => CreateItemType switch
    {
        ContentBrowserViewModel.CreateItemType.CS_SCRIPT => "New C# Script",
        ContentBrowserViewModel.CreateItemType.FOLDER => "New folder",
        ContentBrowserViewModel.CreateItemType.SCENE => "New Scene",
        _ => "Something completely different"
    };
    public ContentBrowserViewModel.CreateItemType CreateItemType { get; private set; }
    public string? EnteredName { get; private set; }

    private ICommand? _okCommand;
    public ICommand OkCommand => _okCommand ??= new ActionCommand(() => Accept());
    private ICommand? _cancelCommand;
    public ICommand CancelCommand => _cancelCommand ??= new ActionCommand(() => Accept());

    public NewItemDialog(ContentBrowserViewModel.CreateItemType createItemType)
    {
        InitializeComponent();
        DataContext = this;
        CreateItemType = createItemType;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void OK_Click(object? _, RoutedEventArgs __)
    {
        Accept();
    }

    private void Cancel_Click(object? _, RoutedEventArgs __)
    {
        Cancel();
    }

    private void Accept()
    {
        EnteredName = ProjectNameTextBox.Text;
        DialogResult = true;
    }

    private void Cancel()
    {
        DialogResult = false;
    }
}
