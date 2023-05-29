gnuplot -p << EOF
set datafile separator ','
set terminal wxt
plot 'carrier1.csv' using 1:4 title 'Original', 'carrier2.csv' using 1:4 title 'Hilbert'
EOF
