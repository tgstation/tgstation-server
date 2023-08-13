#!/bin/sh

# Run with publish directory as parameter
rm -rf $1/runtimes/browser* \
    $1/runtimes/osx* \
    $1/runtimes/mac* \
    $1/runtimes/*-arm* \
    $1/runtimes/*-ppc64le \
    $1/runtimes/*-s390x \
    $1/runtimes/*-mips64
