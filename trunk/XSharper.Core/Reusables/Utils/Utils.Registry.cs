#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace XSharper.Core
{
    public partial class Utils
    {
        /// <summary>
        /// Set registry value, creating the key if neccesary. With support for 32 and 64 bit registries
        /// </summary>
        /// <param name="keyAndValue">Key name in format [\\machineName]RootKey[:32|:64]\subkey\value</param>
        /// <param name="value">Value to set</param>
        /// <returns>previous value, or null if didn't exist</returns>
        public static object RegistrySet(string keyAndValue, object value)
        {
            if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");
            RegistryValueKind kv = RegistryValueKind.Unknown;
            if (value is string[])
                kv = RegistryValueKind.MultiString;
            else if (value is string)
                kv = RegistryValueKind.String;
            else if (value is int || value is uint || value is short || value is ushort || value is byte || value is sbyte)
                kv = RegistryValueKind.DWord;
            else if (value is byte[])
                kv = RegistryValueKind.Binary;
            else if (value is long || value is ulong)
                kv = RegistryValueKind.QWord;

            return RegistrySet(keyAndValue,value,kv);
        }

        /// <summary>
        /// Set registry value, creating the key if neccesary. With support for 32 and 64 bit registries
        /// </summary>
        /// <param name="keyAndValue">Key name in format [\\machineName]RootKey[:32|:64]\subkey\value</param>
        /// <param name="value">Value to set</param>
        /// <param name="kind">Registry value kind</param>
        /// <returns>previous value, or null if didn't exist</returns>
        public static object RegistrySet(string keyAndValue, object value, Microsoft.Win32.RegistryValueKind kind)
        {
            if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");
            object ret = RegistryGet(keyAndValue, null);
            using (RegW64Helper rn = new RegW64Helper(keyAndValue))
            {
                using (var rk=rn.Create(RegistryKeyPermissionCheck.Default))
                {
                    if (value == null)
                        rk.DeleteValue(rn.ValueName,false);
                    else
                        rk.SetValue(rn.ValueName,value,kind);    
                }
                
            }
            return ret;
        }

        /// Get registry value, or return default value with support for 32 and 64 bit registries
        public static object RegistryGet(string keyAndValue, object def, RegistryValueOptions options)
        {
            if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");
            using (RegW64Helper rn = new RegW64Helper(keyAndValue))
            {
                using (var rk = rn.Open(false))
                {
                    if (rk == null)
                        return def;
                    return rk.GetValue(rn.ValueName, def, options);
                }
            }
        }

        /// Get registry value, or return default value with support for 32 and 64 bit registries
        public static object RegistryGet(string keyAndValue, object defaultValue)
        {
            return RegistryGet(keyAndValue, defaultValue, RegistryValueOptions.None);
        }

        /// List all subkeys of a specific key
        public static string[] RegistryGetValueNames(string keyAndValue)
        {
            if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");
            var s = keyAndValue.Trim();
            if (s.Length > 0 && (s[s.Length - 1] != '\\' && s[s.Length - 1] != '/'))
                s += "\\";

            using (RegW64Helper rn = new RegW64Helper(s))
            {
                using (var rk = rn.Open(false))
                {
                    if (rk == null)
                        return null;

                    return rk.GetValueNames();
                }
            }
        }

        /// List all subkeys of a specific key
        public static string[] RegistryGetSubkeyNames(string keyAndValue)
        {
            if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");
            var s = keyAndValue.Trim();
            if (s.Length > 0 && (s[s.Length - 1] != '\\' && s[s.Length - 1] != '/'))
                s += "\\";

            using (RegW64Helper rn = new RegW64Helper(s))
            {
                using (var rk = rn.Open(false))
                {
                    if (rk == null)
                        return null;

                    return rk.GetSubKeyNames();
                }
            }
        }

        /// <summary>
        /// Delete registry value or key. If key is specified, it is deleted recursively.
        /// </summary>
        /// <param name="keyAndValue">Key or value name (key ends with \ or /)</param>
        public static void RegistryDelete(string keyAndValue)
        {
            using (RegW64Helper rn = new RegW64Helper(keyAndValue))
            {
                try
                {
                    if (string.IsNullOrEmpty(rn.ValueName))
                        rn.DeleteTree();
                    else
                        using (var rk = rn.Open(true))
                        {
                            if (rk != null)
                                rk.DeleteValue(rn.ValueName);
                        }
                }
                catch (ArgumentException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        class RegW64Helper : IDisposable
        {
            readonly string _machineName;
            readonly RegistryHive _hive;
            readonly UIntPtr _hiveKey;
            RegistryKey _baseKey;
            string[] _parts;
            NativeMethods.RegSAM? _extraFlags;

            public string KeyName
            {
                get { return string.Join("\\", _parts, 0, _parts.Length - 1); }
            }
            public string ParentKeyName
            {
                get { return string.Join("\\", _parts, 0, _parts.Length - 2); }
            }
            public string ValueName
            {
                get { return _parts[_parts.Length - 1]; }
            }
            public RegW64Helper(string keyAndValue)
            {
                if (keyAndValue == null) throw new ArgumentNullException("keyAndValue");

                _machineName = null;

                var parts = keyAndValue.Split("\\/".ToCharArray());
                int n = 0;
                if (keyAndValue.StartsWith("\\", StringComparison.Ordinal))
                {
                    _machineName = parts[2];
                    n = 3;
                }

                string s = parts[n].ToUpperInvariant();
                if (s.EndsWith(":32",StringComparison.Ordinal))
                {
                    s = s.Substring(0, s.Length - 3);
                    if (Environment.OSVersion.Version.Major > 5)
                        _extraFlags = NativeMethods.RegSAM.WOW64_32Key;
                }
                else if (s.EndsWith(":64", StringComparison.Ordinal))
                {
                    s = s.Substring(0, s.Length - 3);
                    if (Environment.OSVersion.Version.Major > 5)
                        _extraFlags = NativeMethods.RegSAM.WOW64_64Key;
                }

                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    _extraFlags = null;

                switch (s)
                {
                    case "HKEY_LOCAL_MACHINE":
                    case "HKLM":
                        _hive = RegistryHive.LocalMachine;
                        _hiveKey = NativeMethods.HKEY_LOCAL_MACHINE;
                        _baseKey = Registry.LocalMachine;
                        break;
                    case "HKEY_CURRENT_USER":
                    case "HKCU":
                        _hive = RegistryHive.CurrentUser;
                        _hiveKey = NativeMethods.HKEY_CURRENT_USER;
                        _baseKey = Registry.CurrentUser;
                        break;
                    case "HKEY_USERS":
                    case "HKU":
                        _hive = RegistryHive.Users;
                        _hiveKey = NativeMethods.HKEY_USERS;
                        _baseKey = Registry.Users;
                        break;
                    case "HKEY_CLASSES_ROOT":
                    case "HKCR":
                        _hive = RegistryHive.ClassesRoot;
                        _hiveKey = NativeMethods.HKEY_CLASSES_ROOT;
                        _baseKey = Registry.ClassesRoot;
                        break;
                    case "HKEY_PERFORMANCE_DATA":
                        _hive = RegistryHive.PerformanceData;
                        _hiveKey = NativeMethods.HKEY_PERFORMANCE_DATA;
                        _baseKey = Registry.PerformanceData;
                        break;
                    case "HKEY_CURRENT_CONFIG":
                        _hive = RegistryHive.CurrentConfig;
                        _hiveKey = NativeMethods.HKEY_CURRENT_CONFIG;
                        _baseKey = Registry.CurrentConfig;
                        break;
                    case "HKEY_DYN_DATA":
                        _hive = RegistryHive.DynData;
                        _hiveKey = NativeMethods.HKEY_DYN_DATA;
                        _baseKey = Registry.DynData;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("keyAndValue", "Invalid key name '" + parts[0] + "'");
                }
                _parts=new string[parts.Length-n-1];
                Array.Copy(parts,n+1,_parts,0,_parts.Length);
                if (!string.IsNullOrEmpty(_machineName))
                    _baseKey = null;
            }

            
            public RegistryKey Open(bool writable)
            {
                IntPtr hTargetKey;
                return open(KeyName, writable, out hTargetKey);
            }

            public RegistryKey Create(RegistryKeyPermissionCheck permissionCheck)
            {
                if (_extraFlags.HasValue)
                    return createNative(permissionCheck);
                return _baseKey.CreateSubKey(KeyName, permissionCheck);
            }

            private RegistryKey createNative(RegistryKeyPermissionCheck permissionCheck)
            {
                IntPtr hTargetKey;
                NativeMethods.RegSAM flags;
                UIntPtr x = getBaseHKey(true, out flags);

                NativeMethods.RegResult dwDisp;

                int l = NativeMethods.RegCreateKeyEx(x, KeyName, 0, null, NativeMethods.RegOption.NonVolatile, flags, IntPtr.Zero, out hTargetKey, out dwDisp);
                if (l == 0)
                    return fromHandle(hTargetKey, KeyName, true);
                checkSecurityErrorCode(l, KeyName);
                throw new Win32Exception(l);
            }

            private static void checkSecurityErrorCode(int l,string name)
            {
                if ((l == 5) || (l == 0x542))
                    new SecurityException("Access to registry key " + name + " denied");
            }

            ~RegW64Helper()
            {
                Dispose(false);
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected virtual void Dispose(bool dispose)
            {
                if (_machineName != null && _baseKey!=null && dispose)
                {
                    _baseKey.Close();
                    _baseKey = null;
                }
                
            }

            public void DeleteTree()
            {
                if (!_extraFlags.HasValue)
                    _baseKey.DeleteSubKeyTree(KeyName);
                else
                {
                    deleteInternal(KeyName);
                    IntPtr hTargetKey;
                    using (var rk = open(ParentKeyName, true, out hTargetKey))
                    {
                        if (rk != null)
                        {
                            int l=NativeMethods.RegDeleteKey(hTargetKey,_parts[_parts.Length - 2]);
                            checkSecurityErrorCode(l,KeyName);
                        }
                    }
                }
            }

            private void deleteInternal(string name)
            {
                IntPtr hTargetKey;
                using (var rk = open(name, true, out hTargetKey))
                {
                    if (rk==null)
                        return;
                    var k=rk.GetSubKeyNames();
                    foreach (var sub in k)
                    {
                        deleteInternal(rk.Name + "\\" + sub);
                        int l=NativeMethods.RegDeleteKey(hTargetKey, sub);
                        checkSecurityErrorCode(l, rk.Name + "\\" + sub);
                    }
                }
                
            }

            RegistryKey fromHandle(IntPtr key, string kv, bool writable)
            {
                var priv = BindingFlags.Instance | BindingFlags.NonPublic;

                var safeRegistryHandleType = typeof(SafeHandleZeroOrMinusOneIsInvalid).Assembly.GetType("Microsoft.Win32.SafeHandles.SafeRegistryHandle");
                var ctr = safeRegistryHandleType.GetConstructor(priv, null, new[] { typeof(IntPtr), typeof(bool) }, null);
                if (ctr == null)
                {
                    // In .NET4 it's public
                    ctr = safeRegistryHandleType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(IntPtr), typeof(bool) }, null);
                }
                var instance = ctr.Invoke(new object[] { key, true });

                var ctr1 = typeof(RegistryKey).GetConstructor(priv, null, new[] { safeRegistryHandleType, typeof(bool) }, null);
                var ctr2 = typeof(RegistryKey).GetConstructor(priv, null, new[] { typeof(IntPtr), typeof(bool), typeof(bool), typeof(bool), typeof(bool) }, null);

                object r;
                if (ctr2 != null)
                    r = ctr2.Invoke(new object[] { key, writable, false, _machineName != null, _hiveKey == NativeMethods.HKEY_PERFORMANCE_DATA });
                else if (ctr1!=null)
                    r = ctr1.Invoke(new object[] { instance, writable });
                else
                {
                    var mth=typeof(RegistryKey).GetMethod("FromHandle",BindingFlags.Static | BindingFlags.Public, null, new[] { safeRegistryHandleType}, null);
                    r=mth.Invoke(null,new object[] { instance});
                }
                var f = typeof(RegistryKey).GetField("keyName", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null)
                    f.SetValue(r, kv);
                return (RegistryKey)r;
            }


            RegistryKey open(string keyName, bool writable, out IntPtr hTargetKey)
            {
                if (!string.IsNullOrEmpty(_machineName) && _baseKey == null)
                    _baseKey = RegistryKey.OpenRemoteBaseKey(_hive, _machineName);

                hTargetKey = IntPtr.Zero;
                if (_extraFlags.HasValue)
                {
                    NativeMethods.RegSAM flags;
                    UIntPtr x = getBaseHKey(writable, out flags);
                    int l = NativeMethods.RegOpenKeyEx(x, keyName, 0, (uint)flags, out hTargetKey);
                    if (l == 0)
                        return fromHandle(hTargetKey, keyName, writable);
                    if (l == 2)
                        return null;
                    checkSecurityErrorCode(l, KeyName);

                    throw new Win32Exception(l);
                }
                return _baseKey.OpenSubKey(keyName, writable);
            }
            private UIntPtr getBaseHKey(bool writable, out NativeMethods.RegSAM flags)
            {
                flags = NativeMethods.RegSAM.QueryValue | NativeMethods.RegSAM.EnumerateSubKeys | _extraFlags.Value;
                if (writable)
                    flags |= NativeMethods.RegSAM.SetValue | NativeMethods.RegSAM.CreateSubKey | _extraFlags.Value;

                UIntPtr x = _hiveKey;
                if (_machineName != null)
                {
                    var f = typeof(RegistryKey).GetField("hkey", BindingFlags.Instance | BindingFlags.NonPublic);
                    x = new UIntPtr((ulong)((SafeHandle)(f.GetValue(_baseKey))).DangerousGetHandle().ToInt64());
                }
                return x;
            }
        }

    }

    static partial class NativeMethods
    {
        public static UIntPtr HKEY_CLASSES_ROOT = new UIntPtr(0x80000000u);
        public static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
        public static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
        public static UIntPtr HKEY_USERS = new UIntPtr(0x80000003u);
        public static UIntPtr HKEY_PERFORMANCE_DATA = new UIntPtr(0x80000004u);
        public static UIntPtr HKEY_CURRENT_CONFIG = new UIntPtr(0x80000005u);
        public static UIntPtr HKEY_DYN_DATA = new UIntPtr(0x80000006u);


        [Flags]
        public enum RegOption
        {
            NonVolatile = 0x0,
            Volatile = 0x1,
            CreateLink = 0x2,
            BackupRestore = 0x4,
            OpenLink = 0x8
        }

        [Flags]
        public enum RegSAM
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            WOW64_32Key = 0x0200,
            WOW64_64Key = 0x0100,
            WOW64_Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        public enum RegResult
        {
            CreatedNewKey = 0x00000001,
            OpenedExistingKey = 0x00000002
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int RegOpenKeyEx(
          UIntPtr hKey,
          string subKey,
          uint ulOptions,
          uint samDesired,
          out IntPtr hkResult);

        [DllImport("advapi32.dll")]
        public static extern int RegCreateKeyEx(
           UIntPtr hKey,
           string lpSubKey,
           int Reserved,
           string lpClass,
           RegOption dwOptions,
           RegSAM samDesired,
           IntPtr lpSecurityAttributes,
           out IntPtr phkResult,
           out RegResult lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        internal static extern int RegDeleteKey(IntPtr hKey, string lpSubKey);





    }
}