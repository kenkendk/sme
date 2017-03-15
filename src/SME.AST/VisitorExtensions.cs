using System;
using System.Collections.Generic;

namespace SME.AST
{
	/*
	/// <summary>
	/// The visitor state given for each visit
	/// </summary>
	public interface IVisitorState
	{
		/// <summary>
		/// Gets the parents to the current item
		/// </summary>
		List<ASTItem> Parents { get; }
		/// <summary>
		/// The currently visited element
		/// </summary>
		ASTItem Current { get; }
		/// <summary>
		/// Helper method to replace the current item
		/// </summary>
		/// <param name="newitem">The new item to insert instead of the current.</param>
		void ReplaceCurrent(ASTItem newitem);
	}

	/// <summary>
	/// Methods that visit all elements in various parts of the network
	/// </summary>
	public static class VisitorExtensions
	{
		/// <summary>
		/// The visitor method that keeps track of the position in the traversal
		/// </summary>
		private class InternalVisitorState : IVisitorState
		{
			/// <summary>
			/// Gets the parents to the current item.
			/// </summary>
			public List<ASTItem> Parents { get; } = new List<ASTItem>();
			/// <summary>
			/// The currently visited element
			/// </summary>
			public ASTItem Current { get; protected set; }
			/// <summary>
			/// Helper method to replace the current item
			/// </summary>
			/// <param name="newitem">The new item to insert instead of the current.</param>
			public void ReplaceCurrent(ASTItem newitem)
			{
				throw new MissingMethodException();
			}

			/// <summary>
			/// The enter handler.
			/// </summary>
			public readonly Action<IVisitorState> EnterHandler;
			/// <summary>
			/// The visit handler.
			/// </summary>
			public readonly Action<IVisitorState> VisitHandler;
			/// <summary>
			/// The leave handler.
			/// </summary>
			public readonly Action<IVisitorState> LeaveHandler;
			/// <summary>
			/// The predicate function.
			/// </summary>
			public readonly Func<IVisitorState, bool> Predicate;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:SME.AST.VisitorExtensions.InternalVisitorState"/> class.
			/// </summary>
			/// <param name="enter">The enter handler method.</param>
			/// <param name="visit">The visit handler method.</param>
			/// <param name="leave">The leave handler method.</param>
			/// <param name="predicate">The predicate function.</param>
			public InternalVisitorState(Action<IVisitorState> enter, Action<IVisitorState> visit, Action<IVisitorState> leave, Func<IVisitorState, bool> predicate)
			{
				if (enter == null && leave == null && visit == null)
					throw new ArgumentNullException($"Either {nameof(enter)}, {nameof(visit)}, or {nameof(leave)} must be non-null");
				
				EnterHandler = enter;
				LeaveHandler = leave;
				VisitHandler = visit;
				Predicate = predicate ?? (_ => true);
			}

			/// <summary>
			/// The callback method for handling calls from the enumeration
			/// </summary>
			/// <returns><c>true</c>, if the predicate allows continued visit, <c>false</c> otherwise.</returns>
			/// <param name="item">The item being visited.</param>
			/// <param name="state">The visit state.</param>
			public bool VisitCallHandler(ASTItem item, VisitorState state)
			{
				if (state == VisitorState.Enter)
				{
					var hasAdded = Current != null;
					if (hasAdded)
						Parents.Add(Current);

					Current = item;

					if (!Predicate(this))
					{
						if (hasAdded)
							Parents.RemoveAt(Parents.Count - 1);
						return false;
					}

					if (EnterHandler != null)
						EnterHandler(this);
				}
				else if (state == VisitorState.Visit)
				{
					if (VisitHandler != null)
					{
						var tmp = this.Current;
						this.Current = item;
						VisitHandler(this);
						this.Current = tmp;
					}
				}
				else if(state == VisitorState.Leave)
				{
					if (item != Current)
						throw new InvalidOperationException("Attempted to leave item that was not the current one");

					if (Current == null)
						throw new InvalidOperationException($"{Current} was null?");

					if (LeaveHandler != null)
						LeaveHandler(this);

					if (Parents.Count == 0)
						Current = null;
					else
					{
						Current = Parents[Parents.Count - 1];
						Parents.RemoveAt(Parents.Count - 1);
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Visit all elements in the network
		/// </summary>
		/// <param name="network">The network to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Network network, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in network.All(state.VisitCallHandler))
			{ }
		}

		/// <summary>
		/// Visit all process in the network
		/// </summary>
		/// <param name="proc">The process to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Process proc, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in proc.All(state.VisitCallHandler))
			{ }
		}

		/// <summary>
		/// Visit all elements in the bus
		/// </summary>
		/// <param name="bus">The bus to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Bus bus, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in bus.All(state.VisitCallHandler))
			{ }
		}

		/// <summary>
		/// Visit all elements in the method
		/// </summary>
		/// <param name="method">The method to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Method method, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in method.All(state.VisitCallHandler))
			{ }
		}

		/// <summary>
		/// Visit all elements in the statement
		/// </summary>
		/// <param name="statement">The statement to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Statement statement, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in statement.All(state.VisitCallHandler))
			{ }
		}

		/// <summary>
		/// Visit all elements in the expression
		/// </summary>
		/// <param name="expression">The expression to visit.</param>
		/// <param name="enter">The enter handler.</param>
		/// <param name="visit">The visit handler.</param>
		/// <param name="leave">The leave handler.</param>
		/// <param name="predicate">The predicate function.</param>
		public static void Visit(this Expression expression, Action<IVisitorState> enter = null, Action<IVisitorState> visit = null, Action<IVisitorState> leave = null, Func<IVisitorState, bool> predicate = null)
		{
			var state = new InternalVisitorState(enter, visit, leave, predicate);
			foreach (var n in expression.All(state.VisitCallHandler))
			{ }
		}
	}
	*/
}
