using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Tests.Expressions.JS
{
	[TestFixture]
	public class TestCustomFunctions : Util.LuceneTestCase
	{
		private static double DELTA = 0.0000001;

		/// <summary>empty list of methods</summary>
		[Test]
		public virtual void TestEmpty()
		{
			IDictionary<string, MethodInfo> functions = new HashMap<string,MethodInfo>();
			try
			{
				JavascriptCompiler.Compile("sqrt(20)", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("Unrecognized method"));
			}
		}

		/// <summary>using the default map explicitly</summary>
		[Test]
		public virtual void TestDefaultList()
		{
			IDictionary<string, MethodInfo> functions = JavascriptCompiler.DEFAULT_FUNCTIONS;
			var expr = JavascriptCompiler.Compile("sqrt(20)", functions);
			AreEqual(Math.Sqrt(20), expr.Evaluate(0, null), DELTA);
		}

		public static double ZeroArgMethod()
		{
			return 5;
		}

		/// <summary>tests a method with no arguments</summary>
		[Test]
		public virtual void TestNoArgMethod()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("ZeroArgMethod");
			var expr = JavascriptCompiler.Compile("foo()", functions);
			AreEqual(5, expr.Evaluate(0, null), DELTA);
		}

		public static double OneArgMethod(double arg1)
		{
			return 3 + arg1;
		}

		/// <summary>tests a method with one arguments</summary>
		[Test]
		public virtual void TestOneArgMethod()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("OneArgMethod", new []{ typeof(double)});
			var expr = JavascriptCompiler.Compile("foo(3)", functions);
			AreEqual(6, expr.Evaluate(0, null), DELTA);
		}

		public static double ThreeArgMethod(double arg1, double arg2, double arg3)
		{
			return arg1 + arg2 + arg3;
		}

		/// <summary>tests a method with three arguments</summary>
		[Test]
		public virtual void TestThreeArgMethod()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("ThreeArgMethod", new []{ typeof(double), typeof(
				double), typeof(double)});
			var expr = JavascriptCompiler.Compile("foo(3, 4, 5)", functions);
			AreEqual(12, expr.Evaluate(0, null), DELTA);
		}

		/// <summary>tests a map with 2 functions</summary>
		[Test]
		public virtual void TestTwoMethods()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("ZeroArgMethod");
			functions["bar"] = GetType().GetMethod("OneArgMethod", new []{typeof(double)});
			var expr = JavascriptCompiler.Compile("foo() + bar(3)", functions);
			AreEqual(11, expr.Evaluate(0, null), DELTA);
		}

		public static string BogusReturnType()
		{
			return "bogus!";
		}

		/// <summary>wrong return type: must be double</summary>
		[Test]
		public virtual void TestWrongReturnType()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("BogusReturnType");
			try
			{
				JavascriptCompiler.Compile("foo()", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("does not return a double"));
			}
		}

		public static double BogusParameterType(string s)
		{
			return 0;
		}

		/// <summary>wrong param type: must be doubles</summary>
		[Test]
		public virtual void TestWrongParameterType()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("BogusParameterType", new []{ typeof(string)});
			try
			{
				JavascriptCompiler.Compile("foo(2)", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("must take only double parameters"
					));
			}
		}

		public virtual double NonStaticMethod()
		{
			return 0;
		}

		/// <summary>wrong modifiers: must be static</summary>
		[Test]
		public virtual void TestWrongNotStatic()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("NonStaticMethod");
			try
			{
				JavascriptCompiler.Compile("foo()", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("is not static"));
			}
		}

		internal static double NonPublicMethod()
		{
			return 0;
		}

		/// <summary>wrong modifiers: must be public</summary>
		[Test]
		public virtual void TestWrongNotPublic()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = GetType().GetMethod("NonPublicMethod",BindingFlags.NonPublic|BindingFlags.Static);
				
			try
			{
				JavascriptCompiler.Compile("foo()", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("is not public"));
			}
		}

		internal class NestedNotPublic
		{
			public static double Method()
			{
				return 0;
			}
		}

		/// <summary>wrong class modifiers: class containing method is not public</summary>
		[Test]
		public virtual void TestWrongNestedNotPublic()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = typeof(NestedNotPublic).GetMethod("Method");
			try
			{
				JavascriptCompiler.Compile("foo()", functions);
				Fail();
			}
			catch (ArgumentException e)
			{
				IsTrue(e.Message.Contains("is not public"));
			}
		}

		
		internal static string MESSAGE = "This should not happen but it happens";

		public class StaticThrowingException
		{
			public static double Method()
			{
				throw new ArithmeticException(MESSAGE);
			}
		}

		/// <summary>the method throws an exception.</summary>
		/// <remarks>the method throws an exception. We should check the stack trace that it contains the source code of the expression as file name.
		/// 	</remarks>
		[Test]
		public virtual void TestThrowingException()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo"] = typeof(StaticThrowingException).GetMethod("Method");
			string source = "3 * foo() / 5";
			var expr = JavascriptCompiler.Compile(source, functions);
			try
			{
				expr.Evaluate(0, null);
				Fail();
			}
			catch (ArithmeticException e)
			{
				AreEqual(MESSAGE, e.Message);
				StringWriter sw = new StringWriter();
				e.printStackTrace();
                //.NET Port
                IsTrue(e.StackTrace.Contains("Lucene.Net.Expressions.CompiledExpression.Evaluate(Int32 , FunctionValues[] )"));
			}
		}

		/// <summary>test that namespaces work with custom expressions.</summary>
		/// <remarks>test that namespaces work with custom expressions.</remarks>
		[Test]
		public virtual void TestNamespaces()
		{
			IDictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>();
			functions["foo.bar"] = GetType().GetMethod("ZeroArgMethod");
			string source = "foo.bar()";
			var expr = JavascriptCompiler.Compile(source, functions);
			AreEqual(5, expr.Evaluate(0, null), DELTA);
		}
	}
}
