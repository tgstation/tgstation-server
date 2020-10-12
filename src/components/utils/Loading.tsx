import React, { ReactNode } from "react";
import Spinner, { SpinnerProps } from "react-bootstrap/Spinner";
import { FormattedMessage } from "react-intl";
import CSSTransition from "react-transition-group/CSSTransition";
import TransitionGroup from "react-transition-group/TransitionGroup";

type IProps = SpinnerProps & {
    animation: "border" | "grow";
    center: boolean;
    width: number;
    widthUnit: string;
    className?: string;
    text?: string;
};

interface IState {}

export default class Loading extends React.Component<IProps, IState> {
    public static defaultProps = {
        animation: "border",
        width: "50",
        widthUnit: "vmin",
        center: true
    };
    public constructor(props: IProps) {
        super(props);
    }

    public render(): ReactNode {
        const {
            variant,
            animation,
            center,
            className,
            width,
            widthUnit,
            text,
            children,
            ...otherprops
        } = this.props;
        const styles: React.CSSProperties = {
            width: `${width}${widthUnit}`,
            height: `${width}${widthUnit}`
        } as React.CSSProperties;
        return (
            <TransitionGroup>
                <CSSTransition
                    appear
                    classNames="anim-fade"
                    addEndListener={(node, done) => {
                        node.addEventListener("transitionend", done, false);
                    }}>
                    <div className={center ? "text-center" : ""}>
                        <Spinner
                            variant={variant ? variant : "secondary"}
                            className={center ? `d-block mx-auto ${className || ""}` : className}
                            style={styles}
                            animation={animation ? animation : "border"}
                            {...otherprops}
                        />
                        {text ? <FormattedMessage id={text} /> : ""}
                        {children}
                    </div>
                </CSSTransition>
            </TransitionGroup>
        );
    }
}
