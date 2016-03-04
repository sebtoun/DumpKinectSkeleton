using System.Reflection;
using System.Runtime.InteropServices;
using CommandLine;

// Les informations générales relatives à un assembly dépendent de 
// l'ensemble d'attributs suivant. Changez les valeurs de ces attributs pour modifier les informations
// associées à un assembly.
[assembly: AssemblyTitle( "DumpKinectSkeleton" )]
[assembly: AssemblyDescription( "" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "" )]
[assembly: AssemblyProduct( "DumpKinectSkeleton" )]
[assembly: AssemblyCopyright( "Copyright © 2016 Sébastien Andary" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// L'affectation de la valeur false à ComVisible rend les types invisibles dans cet assembly 
// aux composants COM.  Si vous devez accéder à un type dans cet assembly à partir de 
// COM, affectez la valeur true à l'attribut ComVisible sur ce type.
[assembly: ComVisible( false )]

// Le GUID suivant est pour l'ID de la typelib si ce projet est exposé à COM
[assembly: Guid( "923976b8-c255-44e6-b3ed-594478359d47" )]

// Les informations de version pour un assembly se composent des quatre valeurs suivantes :
//
//      Version principale
//      Version secondaire 
//      Numéro de build
//      Révision
//
// Vous pouvez spécifier toutes les valeurs ou indiquer les numéros de build et de révision par défaut 
// en utilisant '*', comme indiqué ci-dessous :
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion( "3.1.*" )]
[assembly: AssemblyFileVersion( "3.1.*" )]


// from .NET class library
[assembly: AssemblyInformationalVersionAttribute( "3.1" )]

// from CommandLineParser.Text
[assembly: AssemblyLicense(
    "This is free software. You may redistribute copies of it under the terms of",
    "the MIT License <http://www.opensource.org/licenses/mit-license.php>." )]
[assembly: AssemblyUsage(
    "Usage: DumpKinectSkeleton [--help] [-v|--video] [-s|--synchronize] [--prefix PREFIX]" )]