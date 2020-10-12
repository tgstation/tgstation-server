const delay = (milliseconds: number): Promise<void> => {
    return new Promise<void>(resolve => {
        setTimeout(resolve, milliseconds);
    });
};

export default delay;
