#!/usr/bin/python
import sys
import os
from shutil import copy

def print_usage():
    print "Usage: " + sys.argv[0] + " [outputdir]"
    print "    copies the binaries from bin/Release to the the specified outputdir (defaults to ~/wgc/core/platform/lib)"

if '-h' in sys.argv or "--help" in sys.argv:
    print_usage()
    sys.exit() # success

if len(sys.argv) == 1:
    target_dir = os.path.expanduser("~/wgc/core/platform/lib")
elif len(sys.argv) == 2:
    target_dir = sys.argv[1]
else:
    print_usage()
    print
    print "You supplied more than 1 argument."
    sys.exit(1)

if os.path.isdir(target_dir):
    source_dir = os.path.join(sys.path[0], "bin", "CodeGenerator", "Release")
    if not os.path.isdir(source_dir):
        print_usage()
        print
        print source_dir + " does not exist. You need to build first."
        sys.exit(3)

    print "Copying binaries from " + source_dir + " to " + target_dir
    for file in os.listdir(source_dir):
        if file.endswith(".exe") or file.endswith(".dll"):
            copy(os.path.join(source_dir, file), target_dir)
            print "    copied: " + file
    print
    print "All files copied. Don't forget to check them in."
else:
    print_usage()
    print
    print target_dir + " is not a directory."
    sys.exit(2)

