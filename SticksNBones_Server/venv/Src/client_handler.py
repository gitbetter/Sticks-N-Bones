import socket
import threading
import sys
import random
import json

from server import *

#
# Client
#   A Client container class that handles client
#   related functionality and stores client information
#

class ClientHandler:
    def __init__(self, sock, addr):
        self.sock = sock
        self.addr = addr
        self.msg_thread = threading.Thread(group=None, target=self.HandleSocketConnection, kwargs={})
        self.msg_thread.start()
        self.command_map = {'match': self.HandleMatchmaking,
                            'exit': self.CloseConnection}

    def HandleSocketConnection(self):
        print("|Sticks N' Bones| - %s:%s has jumped online" % self.addr)
        Server.main().AddClient(self)

        while True:
            try:
                new_message = self.sock.recv(1048).decode()
                self.ProcessMessage(new_message)
            except OSError:
                break

    def ProcessMessage(self, message):
        command, com_arg = message.split(":") if ":" in message else (message, None)
        if command in self.command_map:
           self.command_map[command](com_arg)

    def HandleMatchmaking(self, arg):
        if (arg == "random"):
            opponent = None
            while opponent is None:
                choices = [x for x in Server.main().online_clients if x != self]
                opponent = random.choice(choices) if len(choices) > 0 else None
            self.sock.send(json.dumps({'ip': opponent.addr[0], 'port': opponent.addr[1]}).encode() if opponent else '{}'.encode())

    def CloseConnection(self, arg):
        print("|Sticks N' Bones| - %s:%s jumping offline" % self.addr)
        Server.main().RemoveClient(self)
        self.sock.close()
