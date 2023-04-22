@echo off

echo Hello World!
ping -n 1 127.0.0.1 > NUL
echo Hello Error! 1>&2
