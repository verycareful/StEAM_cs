using StEAM_.NET_main.ViewModels;

namespace StEAM_.NET_main.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
