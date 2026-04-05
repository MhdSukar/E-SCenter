using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ESCenter.Core;
using ESCenter.Services;

namespace ESCenter.Windows
{
    public partial class ExchangeRatesWindow : Window
    {
        private bool _animatingClose;

        public ExchangeRatesWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowFader.SlideIn(this);
            LoadRates();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_animatingClose)
            {
                return;
            }

            e.Cancel = true;
            _animatingClose = true;
            WindowFader.SlideOut(this, () =>
            {
                Hide();
                if (Content is UIElement c)
                {
                    c.RenderTransform = Transform.Identity;
                }

                Opacity = 1;
                Close();
            });
        }

        private void LoadRates()
        {
            var rates = UserPreferencesService.GetExchangeRates();

            UsdRateBox.Text = GetRateText(rates, "USD");
            EurRateBox.Text = GetRateText(rates, "EUR");
            RonRateBox.Text = GetRateText(rates, "RON");
            GbpRateBox.Text = GetRateText(rates, "GBP");
            ChfRateBox.Text = GetRateText(rates, "CHF");
            CadRateBox.Text = GetRateText(rates, "CAD");
            TryRateBox.Text = GetRateText(rates, "TRY");
        }

        private static string GetRateText(Dictionary<string, decimal> rates, string code)
        {
            return rates.TryGetValue(code, out var rate) && rate > 0
                ? rate.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rates = UserPreferencesService.GetExchangeRates();
                rates["USD"] = ParseRate(UsdRateBox.Text, "USD");
                rates["EUR"] = ParseRate(EurRateBox.Text, "EUR");
                rates["RON"] = ParseRate(RonRateBox.Text, "RON");
                rates["GBP"] = ParseRate(GbpRateBox.Text, "GBP");
                rates["CHF"] = ParseRate(ChfRateBox.Text, "CHF");
                rates["CAD"] = ParseRate(CadRateBox.Text, "CAD");
                rates["TRY"] = ParseRate(TryRateBox.Text, "TRY");

                UserPreferencesService.SetExchangeRates(rates);
                AppLogger.Success("Exchange rates saved.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex.Message);
                System.Windows.MessageBox.Show(ex.Message, "Invalid Rate", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static decimal ParseRate(string text, string code)
        {
            if (!decimal.TryParse(text?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) &&
                !decimal.TryParse(text?.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
            {
                throw new InvalidOperationException($"Please enter a valid numeric rate for {code}.");
            }

            if (parsed <= 0)
            {
                throw new InvalidOperationException($"Rate for {code} must be greater than 0.");
            }

            return parsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
