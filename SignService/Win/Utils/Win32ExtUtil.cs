﻿using SignService.Win.Api;
using SignService.Win.Handles;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;

namespace SignService.Win.Utils
{
	internal class Win32ExtUtil
	{
		private static long CRYPT_VERIFYCONTEXT = 0xF0000000;
		private static object internalSyncObject;

		private static SafeProvHandleCP safeGost2001ProvHandle;
		private static SafeProvHandleCP safeGost2012_256ProvHandle;
		private static SafeProvHandleCP safeGost2012_512ProvHandle;
		private static SafeProvHandleCP safeMsProvHandle;

		private static object InternalSyncObject
		{
			[SecurityCritical]
			get
			{
				if (internalSyncObject == null)
				{
					object obj = new object();
					Interlocked.CompareExchange(ref internalSyncObject, obj, null);
				}

				return internalSyncObject;
			}
		}

		/// <summary>
		/// Свойство получения провайдера MS
		/// </summary>
		internal static SafeProvHandleCP StaticMsProvHandle
		{
			[SecurityCritical]
			get
			{
				if (safeMsProvHandle == null)
				{
					lock (InternalSyncObject)
					{
						if (safeMsProvHandle == null)
						{
							SafeProvHandleCP safeProvHandleCP = AcquireProvHandle(new CspParameters(1));
							Thread.MemoryBarrier();
							safeMsProvHandle = safeProvHandleCP;
						}
					}
				}

				return safeMsProvHandle;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hHash"></param>
		/// <returns></returns>
		[SecurityCritical]
		internal static byte[] EndHash(SafeHashHandleCP hHash)
		{
			uint num = 0;
			if (!CApiExtWin.CryptGetHashParam(hHash, 2, null, ref num, 0))
			{
				throw new CryptographicException(Marshal.GetLastWin32Error());
			}

			byte[] numArray = new byte[num];
			if (!CApiExtWin.CryptGetHashParam(hHash, 2, numArray, ref num, 0))
			{
				throw new CryptographicException(Marshal.GetLastWin32Error());
			}

			return numArray;
		}

		/// <summary>
		/// Заполнение данными хэш объекта
		/// </summary>
		/// <param name="hHash"></param>
		/// <param name="data"></param>
		/// <param name="idStart"></param>
		/// <param name="cbSize"></param>
		[SecurityCritical]
		internal static void HashData(SafeHashHandleCP hHash, byte[] data, int idStart, int cbSize)
		{
			try
			{
				byte[] temp = data;
				Array.Copy(data, idStart, temp, 0, cbSize);

				if (!CApiExtWin.CryptHashData(hHash, temp, (uint)cbSize, 0))
				{
					throw new CryptographicException(Marshal.GetLastWin32Error());
				}

				temp = null;
			}
			catch (Exception ex)
			{
				throw new Exception("Ошибка в методе HashData. " + ex.Message);
			}
		}

		/// <summary>
		/// Метод поиска провайдера поддерживающего ГОСТ 3411-2001
		/// </summary>
		internal static SafeProvHandleCP StaticGost2001ProvHandle
		{
			[SecurityCritical]
			get
			{
				if (safeGost2001ProvHandle == null)
				{
					lock (InternalSyncObject)
					{
						if (safeGost2001ProvHandle == null)
						{
							SafeProvHandleCP safeProvHandleCP = AcquireProvHandle(new CspParameters(75));
							Thread.MemoryBarrier();
							safeGost2001ProvHandle = safeProvHandleCP;
						}
					}
				}

				return safeGost2001ProvHandle;
			}
		}

		/// <summary>
		/// Метод поиска провайдера поддерживающего ГОСТ 3410-2012-256
		/// </summary>
		internal static SafeProvHandleCP StaticGost2012_256ProvHandle
		{
			[SecurityCritical]
			get
			{
				if (safeGost2012_256ProvHandle == null)
				{
					lock (InternalSyncObject)
					{
						if (safeGost2012_256ProvHandle == null)
						{
							SafeProvHandleCP safeProvHandleCP = AcquireProvHandle(new CspParameters(80));
							Thread.MemoryBarrier();
							safeGost2012_256ProvHandle = safeProvHandleCP;
						}
					}
				}

				return safeGost2012_256ProvHandle;
			}
		}

		/// <summary>
		/// Метод поиска провайдера поддерживающего ГОСТ 3410-2012-512
		/// </summary>
		internal static SafeProvHandleCP StaticGost2012_512ProvHandle
		{
			[SecurityCritical]
			get
			{
				if (safeGost2012_512ProvHandle == null)
				{
					lock (InternalSyncObject)
					{
						if (safeGost2012_512ProvHandle == null)
						{
							SafeProvHandleCP safeProvHandleCP = AcquireProvHandle(new CspParameters(81));
							Thread.MemoryBarrier();
							safeGost2012_512ProvHandle = safeProvHandleCP;
						}
					}
				}

				return safeGost2012_512ProvHandle;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hProv"></param>
		/// <param name="algid"></param>
		/// <param name="hHash"></param>
		[SecurityCritical]
		internal static void CreateHash(SafeProvHandleCP hProv, int algid, ref SafeHashHandleCP hHash)
		{
			if (!CApiExtWin.CryptCreateHash(hProv, (uint)algid, SafeKeyHandleCP.InvalidHandle, (uint)0, ref hHash))
			{
				throw new CryptographicException(Marshal.GetLastWin32Error());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		[SecurityCritical]
		internal static SafeProvHandleCP AcquireProvHandle(CspParameters parameters)
		{
			if (parameters == null)
			{
				parameters = new CspParameters(75);
			}

			SafeProvHandleCP invalidHandle = SafeProvHandleCP.InvalidHandle;
			AcquireCSP(parameters, ref invalidHandle);

			return invalidHandle;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="param"></param>
		/// <param name="hProv"></param>
		[SecurityCritical]
		internal static void AcquireCSP(CspParameters param, ref SafeProvHandleCP hProv)
		{
			uint num = (uint)CRYPT_VERIFYCONTEXT;// uint.MaxValue; // CRYPT_DEFAULT_CONTAINER_OPTIONAL

			if ((param.Flags & CspProviderFlags.UseMachineKeyStore) != CspProviderFlags.NoFlags)
			{
				num = num | 32;
			}

			if (!CApiExtWin.CryptAcquireContext(ref hProv, param.KeyContainerName, param.ProviderName, (uint)param.ProviderType, num))
			{
				throw new CryptographicException(Marshal.GetLastWin32Error());
			}
		}
	}
}
