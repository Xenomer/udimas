from UDIMAS import *
from System import ValueTuple, Console, Action
from System.Threading import Thread
from System.Threading.Tasks import Task

def booted():
    Thread.Sleep(500)
    Console.Clear()
    Console.CursorVisible = False
    Thread.Sleep(500)
    PrintTo(-1, -3, "UDIMAS", 200)
    PrintTo(-1, Console.WindowHeight / 3 + 1, "Utility Device Independent MAnagement System", 40)
    PrintTo(-1, Console.WindowHeight / 3 + 3, "By Xenomer", 75)
    Thread.Sleep(1000)
    Console.Clear()
    Console.CursorVisible = True

def PrintTo(x, y, msg, interval):
    if x == -1:
        Console.CursorLeft = (Console.WindowWidth - len(msg)) / 2
    else:
        Console.CursorLeft = x
    if y < 0:
        Console.CursorTop = Console.WindowHeight / (y*(-1))
    else:
        Console.CursorTop = y
    for x in msg:
        Console.Write(x)
        Thread.Sleep(interval)
#Udimas.BootComplete += lambda: Task.Run(Action(booted))
