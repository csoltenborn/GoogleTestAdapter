To use picol svgs as icons within VS:
1. Open image.svg with Gimp, choose size 16x16
2. Save as image.png


To stack several icons into one VS resource, use ImageMagick command.

montage source_code.png sitemap.png flag.png viewer_text.png -background none -tile 4x1 -geometry +0+0 PNG32:toolbaricons.png