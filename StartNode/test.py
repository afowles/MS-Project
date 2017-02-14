"""
Author: Bob Krency
File: LSCantorDust.py

Create a Cantor Dust scenario in turtle graphics.
"""

from turtle import *

def main():
    """ n = depth, a = default size """
    reset()
    n = int(input('Please input fractal depth'))
    a = 600
    s = [2]
    Init()
    recurse(s,a,n)

def Init():
    """ Initialize """
    pensize(3)
    up()
    ht()
    speed(0)
    left(90)
    fd(200)
    left(90)
    fd(300)
    left(180)

def CantorLine(s,a,n):
    for i in range(len(s)):
        if s[i] == 2:
            down()
            fd(a)
            up()
        elif s[i] == 1:
            up()
            fd(a)
            down()
    t = newString(s)
    s = t
    resetline()
    recurse(s,a/3,n-1)

def newString(s):
    a = []
    for i in range(len(s)):
        if s[i] == 2:
            b = [2,1,2]
            a.extend(b)
        elif s[i] == 1:
            b = [1,1,1]
            a.extend(b)
    return a

def resetline():
    right(90)
    fd(20)
    left(90)
    back(600)

def recurse(s,a,n):
    if n == 0:
        pass
    else:
        CantorLine(s,a,n)

main()
