To use picol svgs as icons within VS:
1. Open image.svg with Gimp, choose size 16x16
2. Save as image.png


To stack several icons into one VS resource, use ImageMagick command. For three icons:

montage 1.png 2.png 3.png -background none -tile 3x1 -geometry +0+0 PNG32:out.png