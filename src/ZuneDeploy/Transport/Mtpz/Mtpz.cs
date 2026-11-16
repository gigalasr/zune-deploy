/*
    MTPZ trusted-app handshake mostly based on from Android File Transfer for Linux
    (mtp/mtpz/TrustedApp.cpp)

    This library is free software; you can redistribute it and/or modify it
    under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation; either version 2.1 of the License,
    or (at your option) any later version.

    This library is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this library; if not, write to the Free Software Foundation,
    Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

using System.Numerics;
using System.Security.Cryptography;

namespace ZuneDeploy.Transport.Mtpz;

internal sealed class MtpzKeys {
    public required BigInteger Modulus { get; init; }
    public required BigInteger PrivateExponent { get; init; }
    public required byte[] Certificate { get; init; }
    public required int RsaSize { get; init; }

    public static MtpzKeys Load(string path) {
        var lines = File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        var modulus = Convert.FromHexString(lines[2]);
        return new MtpzKeys {
            Modulus = new BigInteger(modulus, isUnsigned: true, isBigEndian: true),
            PrivateExponent = new BigInteger(Convert.FromHexString(lines[3]), isUnsigned: true, isBigEndian: true),
            Certificate = Convert.FromHexString(lines[4]),
            RsaSize = modulus.SkipWhile(b => b == 0).Count(),
        };
    }

    // RSA_private_decrypt/encrypt
    public byte[] GetRawPrivate(ReadOnlySpan<byte> input) {
        var m = BigInteger.ModPow(new BigInteger(input, isUnsigned: true, isBigEndian: true), PrivateExponent, Modulus);
        var bytes = m.ToByteArray(isUnsigned: true, isBigEndian: true);
        var result = new byte[RsaSize];
        bytes.CopyTo(result, RsaSize - bytes.Length);
        return result;
    }
}

internal sealed class MtpzCertificateMessage {
    public required byte[] Challenge { get; init; }
    public required byte[] Message { get; init; }
}

// Building message and crypto of the MTPZ handshake
internal static class MtpzCrypto {
    public static MtpzCertificateMessage GenerateCertificateMessage(MtpzKeys keys, byte[]? fixedChallenge = null) {
        var challenge = fixedChallenge ?? RandomNumberGenerator.GetBytes(16);

        var cert = keys.Certificate;
        var msg = new byte[7 + cert.Length + 2 + challenge.Length + 3 + keys.RsaSize];
        int p = 0;
        msg[p++] = 0x02;
        msg[p++] = 0x01;
        msg[p++] = 0x01;
        msg[p++] = 0x00;
        msg[p++] = 0x00;
        msg[p++] = (byte)(cert.Length >> 8);
        msg[p++] = (byte)cert.Length;
        Buffer.BlockCopy(cert, 0, msg, p, cert.Length);
        p += cert.Length;
        msg[p++] = (byte)(challenge.Length >> 8);
        msg[p++] = (byte)challenge.Length;
        Buffer.BlockCopy(challenge, 0, msg, p, challenge.Length);
        p += challenge.Length;

        var salt = new byte[20 + 8];
        SHA1.HashData(msg.AsSpan(2, p - 2)).CopyTo(salt, 8);
        var hash = SHA1.HashData(salt);

        var key = Hkdf(hash, 107);
        var sig = new byte[keys.RsaSize];
        sig[106] = 1;
        hash.CopyTo(sig, 107);
        for (int i = 0; i < 107; i++) {
            sig[i] ^= key[i];
        }
        sig[0] &= 127;
        sig[127] = 188;

        msg[p++] = 1;
        msg[p++] = 0;
        msg[p++] = (byte)sig.Length;
        keys.GetRawPrivate(sig).CopyTo(msg, p);
        return new MtpzCertificateMessage { Challenge = challenge, Message = msg };
    }

    // Checks answer to our certificate and return CMAC key
    public static byte[] VerifyResponse(MtpzKeys keys, byte[] m, byte[] originalChallenge) {
        int p = 0;
        void Need(byte[] a, int n) {
            if (p + n > a.Length) {
                throw new InvalidDataException("MTPZ: input buffer overrun");
            }
        }

        Need(m, 4);
        if (m[p++] != 2 || m[p++] != 2) {
            throw new InvalidDataException("MTPZ: invalid tag");
        }
        int sigSize = (m[p++] << 8) | m[p++];
        if (sigSize < 0x80 || sigSize != keys.RsaSize) {
            throw new InvalidDataException("MTPZ: invalid signature size");
        }
        Need(m, sigSize);
        var sig = keys.GetRawPrivate(m.AsSpan(p, sigSize));
        p += sigSize;

        var h = Hkdf(sig.AsSpan(21, 107), 20);
        for (int i = 0; i < 20; i++) {
            sig[1 + i] ^= h[i];
        }
        var mask = Hkdf(sig.AsSpan(1, 20), 107);
        for (int i = 0; i < 107; i++) {
            sig[21 + i] ^= mask[i];
        }
        var aesKey = sig[0x70..];

        Need(m, 4);
        if (m[p++] != 0 || m[p++] != 0) {
            throw new InvalidDataException("MTPZ: invalid record");
        }
        int payloadSize = (m[p++] << 8) | m[p++];
        Need(m, payloadSize);
        var payload = AesCbcDecrypt(aesKey, m.AsSpan(p, payloadSize));

        p = 0;
        if (payload[p++] != 1) {
            throw new InvalidDataException("MTPZ: decryption failed");
        }
        int certSize = (payload[p++] << 24) | (payload[p++] << 16) | (payload[p++] << 8) | payload[p++];
        Need(payload, certSize);
        p += certSize;

        Need(payload, 2);
        int challengeSize = (payload[p++] << 8) | payload[p++];
        if (challengeSize != originalChallenge.Length) {
            throw new InvalidDataException("MTPZ: invalid challenge size");
        }
        Need(payload, challengeSize);
        if (!payload.AsSpan(p, challengeSize).SequenceEqual(originalChallenge)) {
            throw new InvalidDataException("MTPZ: challenge does not match");
        }
        p += challengeSize;

        Need(payload, 2);
        challengeSize = (payload[p++] << 8) | payload[p++];
        Need(payload, challengeSize);
        p += challengeSize; // device challenge

        Need(payload, 3);
        if (payload[p++] != 1) {
            throw new InvalidDataException("MTPZ: invalid signature");
        }
        sigSize = (payload[p++] << 8) | payload[p++];
        Need(payload, sigSize);
        p += sigSize;

        Need(payload, 3);
        if (payload[p++] != 1) {
            throw new InvalidDataException("MTPZ: invalid cmac record");
        }
        int cmacSize = (payload[p++] << 8) | payload[p++];
        Need(payload, cmacSize);
        return payload.AsSpan(p, cmacSize).ToArray();
    }

    public static byte[] SignResponse(byte[] cmacKey) {
        var text = new byte[16];
        text[15] = 1;
        var msg = new byte[20];
        msg[0] = 2;
        msg[1] = 3;
        msg[2] = 0;
        msg[3] = 16;
        AesCmac(cmacKey[..16], text).CopyTo(msg, 4);
        return msg;
    }

    // Four CMAC words
    public static uint[] SignSessionRequest(byte[] cmacKey) {
        var s = AesCmac(cmacKey[..16], cmacKey.AsSpan(16, 4));
        var result = new uint[4];
        for (int i = 0; i < 4; i++) {
            result[i] = ((uint)s[i * 4] << 24) | ((uint)s[i * 4 + 1] << 16) | ((uint)s[i * 4 + 2] << 8) | s[i * 4 + 3];
        }
        return result;
    }

    private static byte[] Hkdf(ReadOnlySpan<byte> message, int keySize) {
        const int blockSize = 20;
        int blocks = (keySize + blockSize - 1) / blockSize;
        var key = new byte[blocks * blockSize];
        var counter = new byte[message.Length + 4];
        message.CopyTo(counter);
        for (int i = 0; i < blocks; i++) {
            counter[message.Length] = (byte)(i >> 24);
            counter[message.Length + 1] = (byte)(i >> 16);
            counter[message.Length + 2] = (byte)(i >> 8);
            counter[message.Length + 3] = (byte)i;
            SHA1.HashData(counter).CopyTo(key, i * blockSize);
        }
        return key;
    }

    private static byte[] AesCbcDecrypt(byte[] key, ReadOnlySpan<byte> data) {
        using var aes = Aes.Create();
        aes.Key = key;
        return aes.DecryptCbc(data, new byte[16], PaddingMode.None);
    }

    private static byte[] AesEcb(byte[] key, byte[] block) {
        using var aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptEcb(block, PaddingMode.None);
    }

    private static byte[] AesCmac(byte[] key, ReadOnlySpan<byte> data) {
        static byte[] Double(byte[] b) {
            var r = new byte[16];
            int carry = 0;
            for (int i = 15; i >= 0; i--) {
                r[i] = (byte)((b[i] << 1) | carry);
                carry = (b[i] >> 7) & 1;
            }
            if ((b[0] & 0x80) != 0) {
                r[15] ^= 0x87;
            }
            return r;
        }

        var subkey = Double(AesEcb(key, new byte[16]));
        var block = new byte[16];
        data.CopyTo(block);
        if (data.Length < 16) {
            block[data.Length] = 0x80;
            subkey = Double(subkey);
        }
        for (int i = 0; i < 16; i++) {
            block[i] ^= subkey[i];
        }
        return AesEcb(key, block);
    }
}
