import sys

#
# CL
#   A static Command Line interface for argument processing and other
#   command line related tasks
#

class CL:
    allowableArgs = {'p': {'name': 'port', 'hasValue': True},
                     'v': {'name': 'verbose', 'hasValue': False}}
    args = {}

    @staticmethod
    def ProcessSystemArguments():
        for i in range(0, len(sys.argv)):
            arg = sys.argv[i]
            if (CL.Arg_Valid(arg)):
                if CL.allowableArgs[arg[1]]['hasValue'] and not CL.Arg_ValueSupplied(i):
                    print("Error: no value supplied for argument '%c'" % arg[1])
                    exit(1)

                CL.args[CL.allowableArgs[arg[1]]['name']] = sys.argv[i+1].strip() if CL.allowableArgs[arg[1]]['hasValue'] else True

    @staticmethod
    def Arg_DashIncluded(arg):
        return arg[0] == '-' and len(arg) > 1

    @staticmethod
    def Arg_Supported(arg):
        return arg[1] in CL.allowableArgs

    @staticmethod
    def Arg_Valid(arg):
        return CL.Arg_DashIncluded(arg) and CL.Arg_Supported(arg)

    @staticmethod
    def Arg_ValueSupplied(i):
        return (i+1) < len(sys.argv) and sys.argv[i+1][0] != '-'

    @staticmethod
    def PrintUsage():
        pass