namespace NeuralNetworkProcessor.Core;

public enum ErrorType : uint
{
    NoError = 0,
    //Input Char is not activating any terminal cluster
    //which means not being recoginized
    //This can be either design or input error
    Character = 1,
    //Input activated termianl cluster
    //but not acitivated any grammar part(higher than atom level)
    //This is always due to a design error
    Lexcial = 2,
    //Input activated grammar part, but the 
    //grammar has not activated any higher level
    Syntax = 3
}
