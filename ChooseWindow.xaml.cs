using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Banga_Photobooth
{
    public partial class ChooseWindow : Window
    {
        private readonly List<ImageSource> photos;
        private readonly List<ToggleButton> tileButtons = new();
        private int selectedCount = 0;

        // Resolve named elements via FindName to avoid XAML name generation issues
        private UniformGrid tilesGrid = default!;
        private Button nextButton = default!;

        public IReadOnlyList<ImageSource> SelectedPhotos { get; private set; } = Array.Empty<ImageSource>();

        public ChooseWindow(List<ImageSource> photos)
        {
            InitializeComponent();
            this.photos = photos ?? new List<ImageSource>();
            Loaded += ChooseWindow_Loaded;
        }

        private void ChooseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tilesGrid = (UniformGrid)FindName("TilesGrid");
            nextButton = (Button)FindName("NextButton");
            RenderTiles();
        }

        private void RenderTiles()
        {
            tilesGrid.Children.Clear();
            tileButtons.Clear();

            foreach (var img in photos.Take(6))
            {
                var btn = new ToggleButton
                {
                    Style = (Style)FindResource("PhotoTileToggleStyle"),
                    Content = img,
                };
                btn.Checked += Tile_Checked;
                btn.Unchecked += Tile_Unchecked;
                tileButtons.Add(btn);
                tilesGrid.Children.Add(btn);
            }
        }

        private void Tile_Checked(object sender, RoutedEventArgs e)
        {
            if (selectedCount >= 4)
            {
                // Enforce maximum of 4 selections
                ((ToggleButton)sender).IsChecked = false;
                return;
            }
            selectedCount++;
            UpdateNextEnabled();
        }

        private void Tile_Unchecked(object sender, RoutedEventArgs e)
        {
            if (selectedCount > 0) selectedCount--;
            UpdateNextEnabled();
        }

        private void UpdateNextEnabled()
        {
            nextButton.IsEnabled = (selectedCount == 4);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var chosen = new List<ImageSource>();
            foreach (var btn in tileButtons)
            {
                if (btn.IsChecked == true && btn.Content is ImageSource src)
                    chosen.Add(src);
            }
            SelectedPhotos = chosen;

            // Proceed to the next flow step (placeholder)
            MessageBox.Show($"Selected {SelectedPhotos.Count} photos.", "Selection Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}