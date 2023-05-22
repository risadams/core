// perticula - core - Primes.cs
// 
// Copyright © 2015-2023  Ris Adams - All Rights Reserved
// 
// You may use, distribute and modify this code under the terms of the MIT license
// You should have received a copy of the MIT license with this file. If not, please write to: perticula@risadams.com, or visit : https://github.com/perticula

using core.Cryptography;
using core.Random;

namespace core.Math;

/// <summary>
///   Class Primes.
/// </summary>
public class Primes
{
	/// <summary>
	///   The small factor limit
	/// </summary>
	public static readonly int SmallFactorLimit = 211;

	/// <summary>
	///   The one
	/// </summary>
	private static readonly BigInteger One = BigInteger.One;

	/// <summary>
	///   The two
	/// </summary>
	private static readonly BigInteger Two = BigInteger.Two;

	/// <summary>
	///   The three
	/// </summary>
	private static readonly BigInteger Three = BigInteger.Three;

	/// <summary>
	///   FIPS 186-4 C.6 Shawe-Taylor Random_Prime Routine.
	/// </summary>
	/// <param name="hash">The <see cref="IDigest" /> instance to use (as "Hash()"). Cannot be null.</param>
	/// <param name="length">The length (in bits) of the prime to be generated. Must be at least 2.</param>
	/// <param name="inputSeed">
	///   The seed to be used for the generation of the requested prime. Cannot be null or
	///   empty.
	/// </param>
	/// <returns>An <see cref="ShawTaylorOutput" /> instance containing the requested prime.</returns>
	/// <exception cref="System.ArgumentNullException">hash</exception>
	/// <exception cref="System.ArgumentNullException">inputSeed</exception>
	/// <exception cref="System.ArgumentException">must be >= 2 - length</exception>
	/// <exception cref="System.ArgumentException">cannot be empty - inputSeed</exception>
	/// <remarks>Construct a provable prime number using a hash function.</remarks>
	public static ShawTaylorOutput GenerateRandomPrime(IDigest hash, int length, byte[] inputSeed)
	{
		if (hash             == null) throw new ArgumentNullException(nameof(hash));
		if (length           < 2) throw new ArgumentException("must be >= 2", nameof(length));
		if (inputSeed        == null) throw new ArgumentNullException(nameof(inputSeed));
		if (inputSeed.Length == 0) throw new ArgumentException("cannot be empty", nameof(inputSeed));

		return RandomPrime(hash, length, Arrays.Clone(inputSeed));
	}

	/// <summary>
	///   FIPS 186-4 C.3.2 Enhanced Miller-Rabin Probabilistic Primality Test.
	/// </summary>
	/// <param name="candidate">The <see cref="BigInteger" /> instance to test for primality.</param>
	/// <param name="random">The source of randomness to use to choose bases.</param>
	/// <param name="iterations">The number of randomly-chosen bases to perform the test for.</param>
	/// <returns>An <see cref="MillerRabinOutput" /> instance that can be further queried for details.</returns>
	/// <exception cref="System.ArgumentNullException">random</exception>
	/// <exception cref="System.ArgumentException">must be > 0 - iterations</exception>
	/// <remarks>
	///   Run several iterations of the Miller-Rabin algorithm with randomly-chosen bases. This is an alternative to
	///   <see cref="IsProbablePrime" /> that provides more information about a
	///   composite candidate, which may be useful when generating or validating RSA moduli.
	/// </remarks>
	public static MillerRabinOutput EnhancedProbablePrimeTest(BigInteger candidate, SecureRandom random, int iterations)
	{
		CheckCandidate(candidate, nameof(candidate));

		if (random     == null) throw new ArgumentNullException(nameof(random));
		if (iterations < 1) throw new ArgumentException("must be > 0", nameof(iterations));

		if (candidate.BitLength == 2) return MillerRabinOutput.ProbablyPrime();

		if (!candidate.TestBit(0)) return MillerRabinOutput.ProvablyCompositeWithFactor(Two);

		var w       = candidate;
		var wSubOne = candidate.Subtract(One);
		var wSubTwo = candidate.Subtract(Two);

		var a = wSubOne.GetLowestSetBit();
		var m = wSubOne.ShiftRight(a);

		for (var i = 0; i < iterations; ++i)
		{
			var b = BigIntegers.CreateRandomInRange(Two, wSubTwo, random);
			var g = b.Gcd(w);

			if (g.CompareTo(One) > 0) return MillerRabinOutput.ProvablyCompositeWithFactor(g);

			var z = b.ModPow(m, w);

			if (z.Equals(One) || z.Equals(wSubOne))
				continue;

			var primeToBase = false;

			var x = z;
			for (var j = 1; j < a; ++j)
			{
				z = z.Square().Mod(w);

				if (z.Equals(wSubOne))
				{
					primeToBase = true;
					break;
				}

				if (z.Equals(One))
					break;

				x = z;
			}

			if (!primeToBase)
			{
				if (!z.Equals(One))
				{
					x = z;
					z = z.Square().Mod(w);

					if (!z.Equals(One)) x = z;
				}

				g = x.Subtract(One).Gcd(w);

				return g.CompareTo(One) > 0 ? MillerRabinOutput.ProvablyCompositeWithFactor(g) : MillerRabinOutput.ProvablyCompositeNotPrimePower();
			}
		}

		return MillerRabinOutput.ProbablyPrime();
	}

	/// <summary>
	///   A fast check for small divisors, up to some implementation-specific limit.
	/// </summary>
	/// <param name="candidate">The <see cref="BigInteger" /> instance to test for division by small factors.</param>
	/// <returns><c>true</c> if the candidate is found to have any small factors, <c>false</c> otherwise.</returns>
	public static bool HasAnySmallFactors(BigInteger candidate)
	{
		CheckCandidate(candidate, nameof(candidate));

		/*
		 * Bundle trial divisors into ~32-bit moduli then use fast tests on the ~32-bit remainders.
		 */
		var m = 2 * 3 * 5 * 7 * 11 * 13 * 17 * 19 * 23;
		var r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r    % 2  == 0 || r % 3  == 0 || r % 5  == 0 || r % 7 == 0 || r % 11 == 0 || r % 13 == 0
		    || r % 17 == 0 || r % 19 == 0 || r % 23 == 0)
			return true;

		m = 29 * 31 * 37 * 41 * 43;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 29 == 0 || r % 31 == 0 || r % 37 == 0 || r % 41 == 0 || r % 43 == 0) return true;

		m = 47 * 53 * 59 * 61 * 67;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 47 == 0 || r % 53 == 0 || r % 59 == 0 || r % 61 == 0 || r % 67 == 0) return true;

		m = 71 * 73 * 79 * 83;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 71 == 0 || r % 73 == 0 || r % 79 == 0 || r % 83 == 0) return true;

		m = 89 * 97 * 101 * 103;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 89 == 0 || r % 97 == 0 || r % 101 == 0 || r % 103 == 0) return true;

		m = 107 * 109 * 113 * 127;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 107 == 0 || r % 109 == 0 || r % 113 == 0 || r % 127 == 0) return true;

		m = 131 * 137 * 139 * 149;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 131 == 0 || r % 137 == 0 || r % 139 == 0 || r % 149 == 0) return true;

		m = 151 * 157 * 163 * 167;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 151 == 0 || r % 157 == 0 || r % 163 == 0 || r % 167 == 0) return true;

		m = 173 * 179 * 181 * 191;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 173 == 0 || r % 179 == 0 || r % 181 == 0 || r % 191 == 0) return true;

		m = 193 * 197 * 199 * 211;
		r = candidate.Mod(BigInteger.ValueOf(m)).IntValue;
		if (r % 193 == 0 || r % 197 == 0 || r % 199 == 0 || r % 211 == 0) return true;

		/*
		 * NOTE: Unit tests depend on SMALL_FACTOR_LIMIT matching the
		 * highest small factor tested here.
		 */
		return false;
	}

	/// <summary>
	///   FIPS 186-4 C.3.1 Miller-Rabin Probabilistic Primality Test.
	/// </summary>
	/// <param name="candidate">The <see cref="BigInteger" /> instance to test for primality.</param>
	/// <param name="random">The source of randomness to use to choose bases.</param>
	/// <param name="iterations">The number of randomly-chosen bases to perform the test for.</param>
	/// <returns>
	///   <c>false</c> if any witness to compositeness is found amongst the chosen bases (so
	///   <paramref name="candidate" /> is definitely NOT prime), or else <c>true</c> (indicating primality with some
	///   probability dependent on the number of iterations that were performed).
	/// </returns>
	/// <exception cref="System.ArgumentException">cannot be null - random</exception>
	/// <exception cref="System.ArgumentException">must be > 0 - iterations</exception>
	/// <remarks>Run several iterations of the Miller-Rabin algorithm with randomly-chosen bases.</remarks>
	public static bool IsProbablePrime(BigInteger candidate, SecureRandom random, int iterations)
	{
		CheckCandidate(candidate, nameof(candidate));

		if (random == null)
			throw new ArgumentException("cannot be null", nameof(random));
		if (iterations < 1)
			throw new ArgumentException("must be > 0", nameof(iterations));

		if (candidate.BitLength == 2)
			return true;
		if (!candidate.TestBit(0))
			return false;

		var w       = candidate;
		var wSubOne = candidate.Subtract(One);
		var wSubTwo = candidate.Subtract(Two);

		var a = wSubOne.GetLowestSetBit();
		var m = wSubOne.ShiftRight(a);

		for (var i = 0; i < iterations; ++i)
		{
			var b = BigIntegers.CreateRandomInRange(Two, wSubTwo, random);

			if (!ProbablePrimeToBase(w, wSubOne, m, a, b))
				return false;
		}

		return true;
	}

	/// <summary>
	///   FIPS 186-4 C.3.1 Miller-Rabin Probabilistic Primality Test (to a fixed base).
	/// </summary>
	/// <param name="candidate">The <see cref="BigInteger" /> instance to test for primality.</param>
	/// <param name="baseValue">The base value to use for this iteration.</param>
	/// <returns>
	///   <c>false</c> if <paramref name="baseValue" /> is a witness to compositeness (so
	///   <paramref name="candidate" /> is definitely NOT prime), or else <c>true</c>.
	/// </returns>
	/// <exception cref="System.ArgumentException">
	///   must be < ('candidate' - 1) - baseValue</exception>
	/// <remarks>Run a single iteration of the Miller-Rabin algorithm against the specified base.</remarks>
	public static bool IsProbablePrimeToBase(BigInteger candidate, BigInteger baseValue)
	{
		CheckCandidate(candidate, nameof(candidate));
		CheckCandidate(baseValue, nameof(baseValue));

		if (baseValue.CompareTo(candidate.Subtract(One)) >= 0)
			throw new ArgumentException("must be < ('candidate' - 1)", nameof(baseValue));

		if (candidate.BitLength == 2)
			return true;

		var w       = candidate;
		var wSubOne = candidate.Subtract(One);

		var a = wSubOne.GetLowestSetBit();
		var m = wSubOne.ShiftRight(a);

		return ProbablePrimeToBase(w, wSubOne, m, a, baseValue);
	}

	/// <summary>
	///   Checks the candidate.
	/// </summary>
	/// <param name="n">The n.</param>
	/// <param name="name">The name.</param>
	/// <exception cref="System.ArgumentException">must be non-null and >= 2</exception>
	private static void CheckCandidate(BigInteger n, string name)
	{
		if (n == null || n.SignValue < 1 || n.BitLength < 2)
			throw new ArgumentException("must be non-null and >= 2", name);
	}

	/// <summary>
	///   Probables the prime to base.
	/// </summary>
	/// <param name="w">The w.</param>
	/// <param name="wSubOne">The w sub one.</param>
	/// <param name="m">The m.</param>
	/// <param name="a">a.</param>
	/// <param name="b">The b.</param>
	/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
	private static bool ProbablePrimeToBase(BigInteger w, BigInteger wSubOne, BigInteger m, int a, BigInteger b)
	{
		var z = b.ModPow(m, w);

		if (z.Equals(One) || z.Equals(wSubOne))
			return true;

		for (var j = 1; j < a; ++j)
		{
			z = z.Square().Mod(w);

			if (z.Equals(wSubOne))
				return true;

			if (z.Equals(One))
				return false;
		}

		return false;
	}

	/// <summary>
	///   Randoms the prime.
	/// </summary>
	/// <param name="d">The d.</param>
	/// <param name="length">The length.</param>
	/// <param name="primeSeed">The prime seed.</param>
	/// <returns>ShawTaylorOutput.</returns>
	/// <exception cref="System.InvalidOperationException">Too many iterations in Shawe-Taylor Random_Prime Routine</exception>
	private static ShawTaylorOutput RandomPrime(IDigest d, int length, byte[] primeSeed)
	{
		var dLen = d.GetDigestSize();
		var cLen = System.Math.Max(4, dLen);

		if (length < 33)
		{
			var primeGenCounter = 0;

			var c0 = new byte[cLen];
			var c1 = new byte[cLen];

			for (;;)
			{
				Hash(d, primeSeed, c0, cLen - dLen);
				Inc(primeSeed, 1);

				Hash(d, primeSeed, c1, cLen - dLen);
				Inc(primeSeed, 1);

				var c = Pack.BigEndian_To_UInt32(c0, cLen - 4) ^ Pack.BigEndian_To_UInt32(c1, cLen - 4);
				c &= uint.MaxValue >> (32 - length);
				c |= (1U << (length - 1)) | 1U;

				++primeGenCounter;

				if (IsPrime32(c))
					return new ShawTaylorOutput(BigInteger.ValueOf(c), primeSeed, primeGenCounter);

				if (primeGenCounter > 4 * length)
					throw new InvalidOperationException("Too many iterations in Shawe-Taylor Random_Prime Routine");
			}
		}

		var rec = RandomPrime(d, (length + 3) / 2, primeSeed);
		{
			var c0 = rec.Prime;
			primeSeed = rec.PrimeSeed;
			var primeGenCounter = rec.PrimeGenCounter;

			var outlen     = 8            * dLen;
			var iterations = (length - 1) / outlen;

			var oldCounter = primeGenCounter;

			var x = HashGen(d, primeSeed, iterations + 1);
			x = x.Mod(One.ShiftLeft(length - 1)).SetBit(length - 1);

			var c0X2 = c0.ShiftLeft(1);
			var tx2  = x.Subtract(One).Divide(c0X2).Add(One).ShiftLeft(1);
			var dt   = 0;

			var c = tx2.Multiply(c0).Add(One);

			/*
			 * Since the candidate primes are generated by constant steps ('c0x2'),
			 * sieving could be used here in place of the 'HasAnySmallFactors' approach.
			 */
			for (;;)
			{
				if (c.BitLength > length)
				{
					tx2 = One.ShiftLeft(length - 1).Subtract(One).Divide(c0X2).Add(One).ShiftLeft(1);
					c   = tx2.Multiply(c0).Add(One);
				}

				++primeGenCounter;

				/*
				 * This is an optimization of the original algorithm, using trial division to screen out
				 * many non-primes quickly.
				 * 
				 * NOTE: 'primeSeed' is still incremented as if we performed the full check!
				 */
				if (HasAnySmallFactors(c))
				{
					Inc(primeSeed, iterations + 1);
				}
				else
				{
					var a = HashGen(d, primeSeed, iterations + 1);
					a = a.Mod(c.Subtract(Three)).Add(Two);

					tx2 = tx2.Add(BigInteger.ValueOf(dt));
					dt  = 0;

					var z = a.ModPow(tx2, c);

					if (c.Gcd(z.Subtract(One)).Equals(One) && z.ModPow(c0, c).Equals(One))
						return new ShawTaylorOutput(c, primeSeed, primeGenCounter);
				}

				if (primeGenCounter >= 4 * length + oldCounter)
					throw new InvalidOperationException("Too many iterations in Shawe-Taylor Random_Prime Routine");

				dt += 2;
				c  =  c.Add(c0X2);
			}
		}
	}

	/// <summary>
	///   Hashes the specified d.
	/// </summary>
	/// <param name="d">The d.</param>
	/// <param name="input">The input.</param>
	/// <param name="output">The output.</param>
	/// <param name="outPos">The out position.</param>
	private static void Hash(IDigest d, byte[] input, byte[] output, int outPos)
	{
		d.BlockUpdate(input, 0, input.Length);
		d.DoFinal(output, outPos);
	}

	/// <summary>
	///   Hashes the gen.
	/// </summary>
	/// <param name="d">The d.</param>
	/// <param name="seed">The seed.</param>
	/// <param name="count">The count.</param>
	/// <returns>BigInteger.</returns>
	private static BigInteger HashGen(IDigest d, byte[] seed, int count)
	{
		var dLen = d.GetDigestSize();
		var pos  = count * dLen;
		var buf  = new byte[pos];
		for (var i = 0; i < count; ++i)
		{
			pos -= dLen;
			Hash(d, seed, buf, pos);
			Inc(seed, 1);
		}

		return new BigInteger(1, buf);
	}

	/// <summary>
	///   Incs the specified seed.
	/// </summary>
	/// <param name="seed">The seed.</param>
	/// <param name="c">The c.</param>
	private static void Inc(IList<byte> seed, int c)
	{
		var pos = seed.Count;
		while (c > 0 && --pos >= 0)
		{
			c         +=  seed[pos];
			seed[pos] =   (byte) c;
			c         >>= 8;
		}
	}

	/// <summary>
	///   Determines whether the specified x is prime32.
	/// </summary>
	/// <param name="x">The x.</param>
	/// <returns><c>true</c> if the specified x is prime32; otherwise, <c>false</c>.</returns>
	private static bool IsPrime32(uint x)
	{
		/*
		 * Use wheel factorization with 2, 3, 5 to select trial divisors.
		 */

		if (x < 32) return ((1 << (int) x) & 0b0010_0000_1000_1010_0010_1000_1010_1100) != 0;

		if (((1 << (int) (x % 30U)) & 0b1010_0000_1000_1010_0010_1000_1000_0010U) == 0) return false;

		uint[] ds = {1, 7, 11, 13, 17, 19, 23, 29};
		uint   b  = 0;
		for (var pos = 1;; pos = 0)
		{
			/*
			 * Trial division by wheel-selected divisors
			 */
			while (pos < ds.Length)
			{
				var d = b + ds[pos];
				if (x % d == 0)
					return false;

				++pos;
			}

			b += 30;

			if (b >> 16 != 0 || b * b >= x)
				return true;
		}
	}

	/// <summary>
	///   Used to return the output from the
	///   <see cref="Primes.EnhancedProbablePrimeTest">
	///     Enhanced Miller-Rabin Probabilistic Primality Test
	///   </see>
	/// </summary>
	public sealed class MillerRabinOutput
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="MillerRabinOutput" /> class.
		/// </summary>
		/// <param name="provablyComposite">if set to <c>true</c> [provably composite].</param>
		/// <param name="factor">The factor.</param>
		private MillerRabinOutput(bool provablyComposite, BigInteger? factor)
		{
			IsProvablyComposite = provablyComposite;
			Factor              = factor;
		}

		/// <summary>
		///   Gets the factor.
		/// </summary>
		/// <value>The factor.</value>
		public BigInteger? Factor { get; }

		/// <summary>
		///   Gets a value indicating whether this instance is provably composite.
		/// </summary>
		/// <value><c>true</c> if this instance is provably composite; otherwise, <c>false</c>.</value>
		public bool IsProvablyComposite { get; }

		/// <summary>
		///   Gets a value indicating whether this instance is not prime power.
		/// </summary>
		/// <value><c>true</c> if this instance is not prime power; otherwise, <c>false</c>.</value>
		public bool IsNotPrimePower => IsProvablyComposite && Factor == null;

		/// <summary>
		///   Probablies the prime.
		/// </summary>
		/// <returns>MillerRabinOutput.</returns>
		internal static MillerRabinOutput ProbablyPrime() => new(false, null);

		/// <summary>
		///   Provablies the composite with factor.
		/// </summary>
		/// <param name="factor">The factor.</param>
		/// <returns>MillerRabinOutput.</returns>
		internal static MillerRabinOutput ProvablyCompositeWithFactor(BigInteger factor) => new(true, factor);

		/// <summary>
		///   Provablies the composite not prime power.
		/// </summary>
		/// <returns>MillerRabinOutput.</returns>
		internal static MillerRabinOutput ProvablyCompositeNotPrimePower() => new(true, null);
	}

	/// <summary>
	///   Used to return the output from the
	///   <see cref="Primes.GenerateRandomPrime">
	///     Shawe-Taylor Random_Prime Routine
	///   </see>
	/// </summary>
	public sealed class ShawTaylorOutput
	{
		/// <summary>
		///   Initializes a new instance of the <see cref="ShawTaylorOutput" /> class.
		/// </summary>
		/// <param name="prime">The prime.</param>
		/// <param name="primeSeed">The prime seed.</param>
		/// <param name="primeGenCounter">The prime gen counter.</param>
		internal ShawTaylorOutput(BigInteger prime, byte[] primeSeed, int primeGenCounter)
		{
			Prime           = prime;
			PrimeSeed       = primeSeed;
			PrimeGenCounter = primeGenCounter;
		}

		/// <summary>
		///   Gets the prime.
		/// </summary>
		/// <value>The prime.</value>
		public BigInteger Prime { get; }

		/// <summary>
		///   Gets the prime seed.
		/// </summary>
		/// <value>The prime seed.</value>
		public byte[] PrimeSeed { get; }

		/// <summary>
		///   Gets the prime gen counter.
		/// </summary>
		/// <value>The prime gen counter.</value>
		public int PrimeGenCounter { get; }
	}
}
