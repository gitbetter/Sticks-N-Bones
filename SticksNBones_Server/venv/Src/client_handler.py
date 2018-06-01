import socket
import threading
import sys

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

    def HandleSocketConnection(self):
        while True:
            try:
                new_message = self.sock.recv(1048).decode()
                self.ProcessMessage(new_message)
            except OSError:
                break

    def ProcessMessage(self, message):
        pass