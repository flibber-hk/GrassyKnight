#!/usr/bin/env python3

import sys

lines = []

# This could be done with sed, but there's some relevant differences between
# mac's sed and linux's sed and it just doesn't feel worth it.
with open(sys.argv[1]) as fin:
    for index, line in enumerate(fin):
        if index == 2:
            lines.append(
                "For a nicely formatted version of this README, go to "
                "https://github.com/itsjohncs/GrassyKnight#grassy-knight\n\n")

        lines.append(line)

with open(sys.argv[2]) as fout:
    for line in lines:
        fout.write(line)
