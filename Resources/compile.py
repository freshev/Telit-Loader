"""Routine to "compile" a .py file to a .pyc (or .pyo) file.

This module has intimate knowledge of the format of .pyc files.
"""

import sys
import imp
import os
MAGIC = imp.get_magic()
__debug__ = 0

def wr_long(f, x):
    "Internal; write a 32-bit int to a file in little-endian order."
    f.write(chr( x        & 0xff))
    f.write(chr((x >> 8)  & 0xff))
    f.write(chr((x >> 16) & 0xff))
    f.write(chr((x >> 24) & 0xff))

def compile(file, cfile=None, dfile=None):
    """Byte-compile one Python source file to Python bytecode.

    Arguments:

    file:  source filename
    cfile: target filename; defaults to source with 'c' or 'o' appended
           ('c' normally, 'o' in optimizing mode, giving .pyc or .pyo)
    dfile: purported filename; defaults to source (this is the filename
           that will show up in error messages)

    Note that it isn't necessary to byte-compile Python modules for
    execution efficiency -- Python itself byte-compiles a module when
    it is loaded, and if it can, writes out the bytecode to the
    corresponding .pyc (or .pyo) file.

    However, if a Python installation is shared between users, it is a
    good idea to byte-compile all modules upon installation, since
    other users may not be able to write in the source directories,
    and thus they won't be able to write the .pyc/.pyo file, and then
    they would be byte-compiling every module each time it is loaded.
    This can slow down program start-up considerably.

    See compileall.py for a script/module that uses this module to
    byte-compile all installed files (or all files in selected
    directories).

    """
    import os, marshal, __builtin__
    f = open(file)
    try:
        timestamp = long(os.fstat(f.fileno())[8])
    except AttributeError:
        timestamp = long(os.stat(file)[8])
    codestring = f.read()
    f.close()
    if codestring and codestring[-1] != '\n':
        codestring = codestring + '\n'
    try:
        codeobject = __builtin__.compile(codestring, dfile or file, 'exec')
    except SyntaxError, detail:
        import traceback, sys, string
        lines = traceback.format_exception_only(SyntaxError, detail)
        for line in lines:
            sys.stderr.write(string.replace(line, 'File "<string>"',
                                            'File "%s"' % (dfile or file)))
        if not cfile:
            #cfile = file + (__debug__ and 'c' or 'o')
            #try:
            #    os.remove(cfile) #remove previous pyo/pyc file that may be present...
            #except:
            #    #do nothing...
            #    cfile = ""
            cfile = file + 'e'
            try:
                os.remove(cfile) #remove pye error file that may be present...
            except:
                #do nothing...
                cfile = ""
            cfile = file + 'c'
            try:
                os.remove(cfile) #remove pye error file that may be present...
            except:
                #do nothing...
                cfile = ""
            cfile = file + 'o'
            try:
                os.remove(cfile) #remove pye error file that may be present...
            except:
                #do nothing...
                cfile = ""  
                
            cfile = file + 'e'
        fc = open(cfile, 'wU')
        for line in lines:
            fc.write(string.replace(line, 'File "<string>"','File "%s"' % (dfile or file)))
            fc.write('\n')
        fc.flush()
        fc.close()
        line = lines[0]
        try:
            import string
            errorline =  string.atoi(line[line.find('line')+5:-1])
        except:
            errorline = 1
        return errorline
    
    if not cfile:
        cfile = file + 'e'
        try:
            os.remove(cfile) #remove pye error file that may be present...
        except:
            #do nothing...
            cfile = ""
        cfile = file + 'c'
        try:
            os.remove(cfile) #remove pye error file that may be present...
        except:
            #do nothing...
            cfile = ""
        cfile = file + 'o'
        try:
            os.remove(cfile) #remove pye error file that may be present...
        except:
            #do nothing...
            cfile = ""            
        cfile = file + (__debug__ and 'c' or 'o')
    fc = open(cfile, 'wb')
    fc.write('\0\0\0\0')
    wr_long(fc, timestamp)
    marshal.dump(codeobject, fc)
    fc.flush()
    fc.seek(0, 0)
    fc.write(MAGIC)
    fc.close()
    if os.name == 'mac':
        import macfs
        macfs.FSSpec(cfile).SetCreatorType('Pyth', 'PYC ')
    # now delete the .pye file that may be present..
    return 0

if __name__ == "__main__":
  compile(sys.argv[1])