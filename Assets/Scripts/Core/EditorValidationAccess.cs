#if UNITY_EDITOR
using System.Runtime.CompilerServices;
// Native editor reviews inspect the same production geometry, contacts and
// release state without widening the player build's public gameplay API.
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
#endif
